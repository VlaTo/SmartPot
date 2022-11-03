
#nullable enable

using Android.Bluetooth;
using Android.Content;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using String = System.String;

namespace SmartPot.Application.Core
{
    /// <summary>
    /// 
    /// </summary>
    /*public sealed class ConnectStateEventArgs : EventArgs
    {
        public bool IsConnected
        {
            get;
        }

        public ConnectStateEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }*/

    /// <summary>
    /// 
    /// </summary>
    /*public sealed class RpcResultEventArgs : EventArgs
    {
        public RpcResult RpcResult
        {
            get;
        }

        public RpcResultEventArgs(RpcResult rpcResult)
        {
            RpcResult = rpcResult;
        }
    }*/

    /// <summary>
    /// 
    /// </summary>
    [DebuggerTypeProxy(typeof(ImprovTypeProxy))]
    [DebuggerDisplay("Name = {Name}, Address = {Address}")]
    public sealed partial class ImprovDevice : Java.Lang.Object, IEquatable<ImprovDevice>
    {
        private static readonly UUID? UUID_SERVICE_PROVISION = UUID.FromString("00467768-6228-2272-4663-277478268000");
        private static readonly UUID? UUID_CHARACTERISTIC_CURRENT_STATE = UUID.FromString("00467768-6228-2272-4663-277478268001");
        private static readonly UUID? UUID_CHARACTERISTIC_ERROR_STATE = UUID.FromString("00467768-6228-2272-4663-277478268002");
        private static readonly UUID? UUID_CHARACTERISTIC_RPC_RESULT = UUID.FromString("00467768-6228-2272-4663-277478268004");
        private static readonly UUID? UUID_CHARACTERISTIC_RPC = UUID.FromString("00467768-6228-2272-4663-277478268003");
        private static readonly UUID? UUID_CHARACTERISTIC_CAPABILITIES = UUID.FromString("00467768-6228-2272-4663-277478268005");
        //private static readonly UUID? UUID_DESCRIPTOR_ENABLE = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        private static readonly UUID? UUID_SUBSCRIPTION_DESCRIPTOR = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");

        private readonly ImprovManager manager;
        private readonly Context context;
        private readonly WorkItemQueue queue;
        private readonly BluetoothDevice device;
        private readonly BluetoothCallback bluetoothCallback;
        private readonly Stack<ManualResetEventSlim> waiters;
        private BluetoothGatt? bluetoothGatt;
        private BluetoothGattService? provisionService;
        private BluetoothGattCharacteristic? currentStateCharacteristic;
        private BluetoothGattCharacteristic? errorStateCharacteristic;
        private BluetoothGattCharacteristic? rpcCommandCharacteristic;
        private BluetoothGattCharacteristic? rpcResultCharacteristic;
        private BluetoothGattCharacteristic? capabilitiesCharacteristic;
        private SequenceStage stage;

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

        public RpcResult RpcResult
        {
            get;
            private set;
        }

        public bool IsConnected => SequenceStage.Connected == stage;

        public event EventHandler? ConnectStateChanged;

        public event EventHandler? RpcResultChanged;

        internal ImprovDevice(ImprovManager manager, Context context, WorkItemQueue queue, BluetoothDevice device)
        {
            this.manager = manager;
            this.context = context;
            this.queue = queue;
            this.device = device;
            stage = SequenceStage.Disconnected;
            waiters = new Stack<ManualResetEventSlim>();
            bluetoothCallback = new BluetoothCallback
            {
                ConnectionStateChange = OnConnectionStateChange,
                ServicesDiscovered = OnServicesDiscovered,
                CharacteristicChanged = OnCharacteristicChanged,
                CharacteristicRead = OnCharacteristicRead,
                CharacteristicWrite = OnCharacteristicWrite,
                DescriptorRead = OnDescriptorRead,
                DescriptorWrite = OnDescriptorWrite
            };
            DeviceState = DeviceState.AuthorizationRequired;
            ErrorCode = ErrorCode.NoError;
        }

        public void Connect()
        {
            if (SequenceStage.Connected == stage)
            {
                ;
            }

            if (SequenceStage.Disconnected == stage)
            {
                queue.Enqueue(new Runnable(ConnectSequence));
            }
        }

        public void SendCredentials(string ssid, string? password)
        {
            if (SequenceStage.Connected != stage)
            {
                throw new InvalidOperationException();
            }

            queue.Enqueue(new Runnable<string, string?>(SendCredentialsSequence, ssid, password));
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

        private void RaiseConnectStateChangedEvent(EventArgs e)
        {
            var handler = ConnectStateChanged;

            if (null != handler)
            {
                handler.Invoke(this, e);
            }
        }

        private void RaiseRpcResultChangedEvent(EventArgs e)
        {
            var handler = RpcResultChanged;

            if (null != handler)
            {
                handler.Invoke(this, e);
            }
        }

        #region RPC commands

        public enum RpcCommand : byte
        {
            Unknown = 0,
            SendCredentials = 1,
            Identity = 2
        }

        #endregion

        #region Internal sequence stages

        private enum SequenceStage
        {
            Failed = -1,
            Disconnected,
            BluetoothGattConnecting,
            BluetoothGattAcquired,
            DiscoveringServices,
            GetCurrentStateCharacteristic,
            ReadCurrentStateCharacteristic,
            SubscribeCurrentStateCharacteristic,
            WriteCurrentStateDescriptor,
            GetErrorStateCharacteristic,
            ReadErrorStateCharacteristic,
            SubscribeErrorStateCharacteristic,
            WriteErrorStateDescriptor,
            GetRpcResultCharacteristic,
            ReadRpcResultCharacteristic,
            SubscribeRpcResultCharacteristic,
            WriteRpcResultDescriptor,
            GetRpcCommandCharacteristic,
            GetCapabilitiesCharacteristic,
            ReadCapabilitiesCharacteristic,

            SendRpcRequest,
            WaitRpcResult,
            RpcCompleted,

            Connected
        }

        #endregion

        #region Debugger TypeProxy

        private sealed class ImprovTypeProxy
        {
            
        }

        #endregion
    }
}

#nullable restore