
#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Android.Bluetooth;
using Android.Content;
using Java.Util;
using String = System.String;

namespace SmartPot.Application.Core
{
    /// <summary>
    /// 
    /// </summary>
    [DebuggerTypeProxy(typeof(ImprovTypeProxy))]
    [DebuggerDisplay("Name = {Name}, Address = {Address}")]
    public sealed class ImprovDevice : BluetoothGattCallback, IEquatable<ImprovDevice>
    {
        private static readonly UUID? UUID_SERVICE_PROVISION = UUID.FromString("00467768-6228-2272-4663-277478268000");
        private static readonly UUID? UUID_CHARACTERISTIC_CURRENT_STATE = UUID.FromString("00467768-6228-2272-4663-277478268001");
        private static readonly UUID? UUID_CHARACTERISTIC_ERROR_STATE = UUID.FromString("00467768-6228-2272-4663-277478268002");
        private static readonly UUID? UUID_CHARACTERISTIC_RPC_RESULT = UUID.FromString("00467768-6228-2272-4663-277478268004");
        private static readonly UUID? UUID_CHARACTERISTIC_RPC = UUID.FromString("00467768-6228-2272-4663-277478268003");

        public interface IImprovCallback
        {
            void OnConnected(bool connected);

            void OnCredentialsSent();
        }

        private readonly ImprovManager manager;
        private readonly Context context;
        private readonly WorkItemQueue queue;
        private readonly BluetoothDevice device;
        private BluetoothGatt? bluetoothGatt;
        private BluetoothGattService? bluetoothGattService;
        private BluetoothGattCharacteristic? currentStateCharacteristic;
        private BluetoothGattCharacteristic? errorStateCharacteristic;
        private BluetoothGattCharacteristic? rpcCommandCharacteristic;
        private BluetoothGattCharacteristic? rpcResultCharacteristic;
        private IImprovCallback? onConnectedCallback;
        private State state;

        public string? Name => device.Name;

        public string? Address => device.Address;

        public DeviceState DeviceState
        {
            get;
            private set;
        }

        public ErrorCode ErrorCode
        {
            get;
            private set;
        }

        internal ImprovDevice(ImprovManager manager, Context context, WorkItemQueue queue, BluetoothDevice device)
        {
            this.manager = manager;
            this.context = context;
            this.queue = queue;
            this.device = device;
            state = State.NotConnected;
            DeviceState = DeviceState.AuthorizationRequired;
            ErrorCode = ErrorCode.NoError;
        }

        public bool IsSame(BluetoothDevice bluetoothDevice)
        {
            if (ReferenceEquals(bluetoothDevice, null))
            {
                return false;
            }

            if (ReferenceEquals(device, bluetoothDevice))
            {
                return true;
            }

            return String.Equals(device.Address, bluetoothDevice.Address);
        }

        public void Connect(IImprovCallback callback)
        {
            if (State.Failed == state)
            {
                callback.OnConnected(false);
            }

            onConnectedCallback = callback;

            if (State.NotConnected == state)
            {
                state = State.Connecting;
                queue.Enqueue(new Runnable(DoDeviceConnect));
            }
        }

        public void SendCredentials(string ssid, string? password)
        {
            if (State.Connected != state)
            {
                return;
            }

            queue.Enqueue(new Runnable<string, string?>(DoSendCredentials, ssid, password));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(0x351D, Name, Address);
        }

        public override string ToString()
        {
            return $"{{Name: {Name}, Address: {Address}}}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return obj is ImprovDevice improvDevice && Equals(improvDevice);
        }

        public bool Equals(ImprovDevice other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return String.Equals(Name, other.Name) && String.Equals(Address, other.Address);
        }

        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            if (GattStatus.Success != status)
            {
                return;
            }

            if (ProfileState.Connected != newState)
            {
                return;
            }

            if (State.Connecting == state)
            {
                if (null != gatt)
                {
                    bluetoothGatt = gatt;
                    bluetoothGatt.DiscoverServices();

                    return;
                }
            }
            else
            {
                ;
            }

            state = State.Failed;
        }

        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);

            if (GattStatus.Success != status)
            {
                return;
            }

            bluetoothGattService = bluetoothGatt?.GetService(UUID_SERVICE_PROVISION);

            if (null != bluetoothGattService)
            {
                currentStateCharacteristic = bluetoothGattService.GetCharacteristic(UUID_CHARACTERISTIC_CURRENT_STATE);

                if (null != currentStateCharacteristic)
                {
                    queue.Enqueue(new Runnable<BluetoothGattCharacteristic>(DoReadCharacteristic, currentStateCharacteristic));
                }
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            base.OnCharacteristicRead(gatt, characteristic, status);
            
            Debug.WriteLine($"Characteristic read: {characteristic?.Uuid?.ToString() ?? "(null)"}");

            if (GattStatus.Success != status)
            {
                return;
            }

            if (null == characteristic)
            {
                return;
            }

            if (characteristic.Uuid?.Equals(UUID_CHARACTERISTIC_CURRENT_STATE) ?? false)
            {
                var value = characteristic.GetIntValue(GattFormat.Sint8, 0);

                if (null == value)
                {
                    return;
                }

                DeviceState = (DeviceState)value.ByteValue();

                switch (state)
                {
                    case State.Connecting:
                    {
                        queue.Enqueue(new Runnable(DoSubscribeCurrentState));
                        break;
                    }

                    case State.Connected:
                    {
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
            else if(characteristic.Uuid?.Equals(UUID_CHARACTERISTIC_ERROR_STATE) ?? false)
            {
                var value = characteristic.GetIntValue(GattFormat.Sint8, 0);

                if (null == value)
                {
                    return;
                }

                ErrorCode = (ErrorCode)value.ByteValue();

                switch (state)
                {
                    case State.Connecting:
                    {
                        queue.Enqueue(new Runnable(DoSubscribeErrorState));
                        break;
                    }

                    case State.Connected:
                    {
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
            else if(characteristic.Uuid?.Equals(UUID_CHARACTERISTIC_RPC_RESULT) ?? false)
            {
                switch (state)
                {
                    case State.Connecting:
                    {
                        queue.Enqueue(new Runnable(DoSubscribeRpcResult));
                        break;
                    }

                    case State.Connected:
                    {
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
        }

        public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            base.OnCharacteristicWrite(gatt, characteristic, status);

            Debug.WriteLine($"Characteristic write: {characteristic?.Uuid?.ToString() ?? "(null)"}");

            if (State.Failed == state)
            {
                return;
            }

            if (characteristic?.Uuid?.Equals(UUID_CHARACTERISTIC_RPC) ?? false)
            {
                //var value = characteristic.SetValue();
                Debug.WriteLine("RPC Command");
            }
            else if (characteristic?.Uuid?.Equals(UUID_CHARACTERISTIC_RPC_RESULT) ?? false)
            {
                Debug.WriteLine("RPC Result");
            }
        }

        public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);
            
            Debug.WriteLine($"Characteristic changed: {characteristic?.Uuid?.ToString() ?? "(null)"}");

            if (characteristic?.Uuid?.Equals(UUID_CHARACTERISTIC_CURRENT_STATE) ?? false)
            {
                var value = characteristic.GetIntValue(GattFormat.Sint8, 0);

                if (null == value)
                {
                    return;
                }

                switch (state)
                {
                    case State.Connecting:
                    {
                        break;
                    }

                    case State.Connected:
                    {
                        DeviceState = (DeviceState)value.ByteValue();
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
            else if (characteristic?.Uuid?.Equals(UUID_CHARACTERISTIC_ERROR_STATE) ?? false)
            {
                var value = characteristic.GetIntValue(GattFormat.Sint8, 0);

                if (null == value)
                {
                    return;
                }

                switch (state)
                {
                    case State.Connecting:
                    {
                        break;
                    }

                    case State.Connected:
                    {
                        ErrorCode = (ErrorCode)value.ByteValue();
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
            else if (characteristic?.Uuid?.Equals(UUID_CHARACTERISTIC_RPC_RESULT) ?? false)
            {
                switch (state)
                {
                    case State.Connecting:
                    {
                        break;
                    }

                    case State.Connected:
                    {
                        var bytes = characteristic.GetValue();

                        if (null != onConnectedCallback)
                        {
                            onConnectedCallback.OnCredentialsSent();
                        }

                        break;
                    }

                    default:
                    {
                        break;
                    }
                }
            }
        }

        private void DoDeviceConnect()
        {
            device.ConnectGatt(context, true, this);
        }

        private void DoReadCharacteristic(BluetoothGattCharacteristic characteristic)
        {
            var success = bluetoothGatt!.ReadCharacteristic(characteristic);

            if (false == success)
            {
                state = State.Failed;
                return;
            }

            ;
        }

        private void DoSubscribeCurrentState()
        {
            var success = bluetoothGatt!.SetCharacteristicNotification(currentStateCharacteristic, true);

            if (success)
            {
                var descriptor = currentStateCharacteristic?.Descriptors?.FirstOrDefault();

                if (null != descriptor)
                {
                    var enable = BluetoothGattDescriptor.EnableNotificationValue?.ToArray();
                    descriptor.SetValue(enable);
                }

                errorStateCharacteristic = bluetoothGattService!.GetCharacteristic(UUID_CHARACTERISTIC_ERROR_STATE);

                if (null != errorStateCharacteristic)
                {
                    queue.Enqueue(new Runnable<BluetoothGattCharacteristic>(DoReadCharacteristic, errorStateCharacteristic));
                }
                else
                {
                    state = State.Failed;
                }
            }
            else
            {
                state = State.Failed;
            }
        }

        private void DoSubscribeErrorState()
        {
            var success = bluetoothGatt!.SetCharacteristicNotification(errorStateCharacteristic, true);

            if (success)
            {
                var descriptor = errorStateCharacteristic?.Descriptors?.FirstOrDefault();

                if (null != descriptor)
                {
                    var enable = BluetoothGattDescriptor.EnableNotificationValue?.ToArray();
                    descriptor.SetValue(enable);
                }

                rpcResultCharacteristic = bluetoothGattService!.GetCharacteristic(UUID_CHARACTERISTIC_RPC_RESULT);

                if (null != rpcResultCharacteristic)
                {
                    queue.Enqueue(new Runnable<BluetoothGattCharacteristic>(DoReadCharacteristic, rpcResultCharacteristic));
                }
                else
                {
                    state = State.Failed;
                }
            }
            else
            {
                state = State.Failed;
            }
        }

        private void DoSubscribeRpcResult()
        {
            var success = bluetoothGatt!.SetCharacteristicNotification(rpcResultCharacteristic, true);

            if (success)
            {
                var descriptor = rpcResultCharacteristic?.Descriptors?.FirstOrDefault();

                if (null != descriptor)
                {
                    var enable = BluetoothGattDescriptor.EnableNotificationValue?.ToArray();
                    descriptor.SetValue(enable);
                }

                rpcCommandCharacteristic = bluetoothGattService!.GetCharacteristic(UUID_CHARACTERISTIC_RPC);

                if (null != rpcCommandCharacteristic)
                {
                    state = State.Connected;
                    onConnectedCallback?.OnConnected(true);
                }
            }
            else
            {
                state = State.Failed;
            }
        }

        private void DoSendCredentials(string ssid, string? password)
        {
            var payload = new Payload();

            payload.Write(ssid, Encoding.UTF8);
            payload.Write(password, Encoding.UTF8);

            SendRpcCommand(RpcCommand.SendCredentials, payload.Build(false));
        }

        private void SendRpcCommand(RpcCommand command, byte[] data)
        {
            var payload = BuildPayload(command, data);

            if (null != rpcCommandCharacteristic)
            {
                rpcCommandCharacteristic.SetValue(payload.ToArray());
                queue.Enqueue(new Runnable(DoSendRpc));
            }
        }

        private void DoSendRpc()
        {
            var success = bluetoothGatt!.WriteCharacteristic(rpcCommandCharacteristic);

            if (false == success)
            {
                return;
            }

            Debug.WriteLine("RPC write success");
        }

        private static byte[] BuildPayload(RpcCommand command, Span<byte> data)
        {
            var payload = new Payload()
                .Write((byte)command)
                .Write(data);

            return payload.Build(true);
        }

        private enum State
        {
            Failed = -1,
            NotConnected,
            Connecting,
            Connected
        }

        private enum RpcCommand : byte
        {
            SendCredentials = 1,
            Identity = 2
        }

        #region Debugger TypeProxy

        private sealed class ImprovTypeProxy
        {
            
        }

        #endregion
    }
}

#nullable restore