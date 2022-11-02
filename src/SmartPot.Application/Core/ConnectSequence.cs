
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Android.Bluetooth;
using Android.Content;
using Java.Util;

namespace SmartPot.Application.Core
{
    internal sealed class ConnectSequence : BluetoothGattCallback, Java.Lang.IRunnable
    {
        private static readonly UUID? UUID_SERVICE_PROVISION = UUID.FromString("00467768-6228-2272-4663-277478268000");
        private static readonly UUID? UUID_CHARACTERISTIC_CURRENT_STATE = UUID.FromString("00467768-6228-2272-4663-277478268001");
        private static readonly UUID? UUID_CHARACTERISTIC_ERROR_STATE = UUID.FromString("00467768-6228-2272-4663-277478268002");
        private static readonly UUID? UUID_CHARACTERISTIC_RPC_RESULT = UUID.FromString("00467768-6228-2272-4663-277478268004");
        private static readonly UUID? UUID_CHARACTERISTIC_RPC = UUID.FromString("00467768-6228-2272-4663-277478268003");
        private static readonly UUID? UUID_CHARACTERISTIC_CAPABILITIES = UUID.FromString("00467768-6228-2272-4663-277478268005");
        private static readonly UUID? UUID_SUBSCRIPTION_DESCRIPTOR = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");

        private readonly BluetoothDevice device;
        private readonly Context context;
        private readonly Action<bool> callback;
        private readonly Stack<ManualResetEventSlim> waiters;
        private BluetoothGatt? bluetoothGatt;
        private BluetoothGattService? provisionService;
        private BluetoothGattCharacteristic? currentStateCharacteristic;
        private BluetoothGattCharacteristic? errorStateCharacteristic;
        private BluetoothGattCharacteristic? rpcResultCharacteristic;
        private BluetoothGattCharacteristic? rpcCommandCharacteristic;
        private BluetoothGattCharacteristic? capabilitiesCharacteristic;
        private SequenceStage stage;

        public ConnectSequence(BluetoothDevice device, Context context, Action<bool> callback)
        {
            this.device = device;
            this.context = context;
            this.callback = callback;
            stage = SequenceStage.NotStarted;
            waiters = new Stack<ManualResetEventSlim>();
        }

        public void Run()
        {
            bool CanProceed() => SequenceStage.Complete != stage && SequenceStage.Failed != stage;

            while (CanProceed())
            {
                switch (stage)
                {
                    case SequenceStage.NotStarted:
                    {
                        var manualResetEventSlim = new ManualResetEventSlim();

                        stage = SequenceStage.BluetoothGattConnecting;
                        waiters.Push(manualResetEventSlim);

                        var gatt = device.ConnectGatt(context, true, this);

                        if (null != gatt)
                        {
                            Debug.WriteLine("SequenceStage.NotStarted get BluetoothGatt");
                        }

                        stage = manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d))
                            ? SequenceStage.BluetoothGattAcquired
                            : SequenceStage.Failed;

                        break;
                    }

                    case SequenceStage.BluetoothGattConnecting:
                    {

                        break;
                    }

                    case SequenceStage.BluetoothGattAcquired:
                    {
                        if (null != bluetoothGatt)
                        {
                            var manualResetEventSlim = new ManualResetEventSlim();

                            stage = SequenceStage.DiscoveringServices;
                            waiters.Push(manualResetEventSlim);

                            var success = bluetoothGatt.DiscoverServices();

                            if (success)
                            {
                                Debug.WriteLine("Start discover services...");
                            }

                            if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                            {
                                //stage = SequenceStage.GetCurrentStateCharacteristic;
                                stage = SequenceStage.GetCapabilitiesCharacteristic;
                            }
                            else
                            {
                                Debug.WriteLine("Discovering service timed out");
                                stage = SequenceStage.Failed;
                            }
                        }

                        break;
                    }

                    case SequenceStage.GetCurrentStateCharacteristic:
                    {
                        if (null != provisionService)
                        {
                            var characteristic = provisionService.GetCharacteristic(UUID_CHARACTERISTIC_CURRENT_STATE);

                            if (null != characteristic)
                            {
                                var manualResetEventSlim = new ManualResetEventSlim();

                                stage = SequenceStage.ReadCurrentStateCharacteristic;
                                waiters.Push(manualResetEventSlim);

                                currentStateCharacteristic = characteristic;

                                var success = bluetoothGatt!.ReadCharacteristic(currentStateCharacteristic);

                                if (success)
                                {
                                    if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                                    {
                                        var value = currentStateCharacteristic.GetIntValue(GattFormat.Sint8, 0);

                                        if (null != value)
                                        {
                                            var deviceState = (DeviceState)value.ByteValue();

                                            Debug.WriteLine($"Read Current state: {deviceState}");

                                            stage = SequenceStage.SubscribeCurrentStateCharacteristic;
                                        }
                                        else
                                        {
                                            Debug.WriteLine("Read Current state invalid format");
                                            stage = SequenceStage.Failed;
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Read Current state timed out");
                                        stage = SequenceStage.Failed;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("Read Current failed");
                                    stage = SequenceStage.Failed;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("No Current state characteristic");
                                stage = SequenceStage.Failed;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("No Provision service");
                            stage = SequenceStage.Failed;
                        }

                        break;
                    }

                    case SequenceStage.SubscribeCurrentStateCharacteristic:
                    {
                        if (null != currentStateCharacteristic)
                        {
                            var success = bluetoothGatt!.SetCharacteristicNotification(currentStateCharacteristic, true);

                            if (success)
                            {
                                var descriptor = currentStateCharacteristic.GetDescriptor(UUID_SUBSCRIPTION_DESCRIPTOR);

                                if (null != descriptor)
                                {
                                    var manualResetEventSlim = new ManualResetEventSlim();
                                    var enable = GetEnableDescriptorValue();

                                    stage = SequenceStage.WriteCurrentStateDescriptor;
                                    waiters.Push(manualResetEventSlim);

                                    var result = descriptor.SetValue(enable) && bluetoothGatt!.WriteDescriptor(descriptor);

                                    if (result)
                                    {
                                        ;
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Failed set/write descriptor");
                                        stage = SequenceStage.Failed;
                                    }

                                    if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                                    {
                                        stage = SequenceStage.GetErrorStateCharacteristic;
                                    }
                                    else
                                    {
                                        Debug.WriteLine("set/write descriptor timed out");
                                        stage = SequenceStage.Failed;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("No subscription descriptor");
                                    stage = SequenceStage.Failed;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Failed set notification");
                                stage = SequenceStage.Failed;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("No Current state characteristic");
                            stage = SequenceStage.Failed;
                        }

                        break;
                    }

                    case SequenceStage.WriteCurrentStateDescriptor:
                    {

                        break;
                    }

                    case SequenceStage.GetErrorStateCharacteristic:
                    {
                        if (null != provisionService)
                        {
                            var characteristic = provisionService.GetCharacteristic(UUID_CHARACTERISTIC_ERROR_STATE);

                            if (null != characteristic)
                            {
                                var manualResetEventSlim = new ManualResetEventSlim();

                                stage = SequenceStage.ReadErrorStateCharacteristic;
                                waiters.Push(manualResetEventSlim);

                                errorStateCharacteristic = characteristic;

                                var success = bluetoothGatt!.ReadCharacteristic(errorStateCharacteristic);

                                if (success)
                                {
                                    if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                                    {
                                        var value = errorStateCharacteristic.GetIntValue(GattFormat.Sint8, 0);

                                        if (null != value)
                                        {
                                            var errorCode = (ErrorCode)value.ByteValue();

                                            Debug.WriteLine($"Read Error code: {errorCode}");

                                            stage = SequenceStage.SubscribeErrorStateCharacteristic;
                                        }
                                        else
                                        {
                                            Debug.WriteLine("Read Error code invalid format");
                                            stage = SequenceStage.Failed;
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Read Error code timed out");
                                        stage = SequenceStage.Failed;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("Read Error code failed");
                                    stage = SequenceStage.Failed;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("No Error code characteristic");
                                stage = SequenceStage.Failed;
                            }
                        }

                        break;
                    }

                    case SequenceStage.ReadErrorStateCharacteristic:
                    {

                        break;
                    }

                    case SequenceStage.SubscribeErrorStateCharacteristic:
                    {
                        if (null != errorStateCharacteristic)
                        {
                            var success = bluetoothGatt!.SetCharacteristicNotification(errorStateCharacteristic, true);

                            if (success)
                            {
                                var descriptor = errorStateCharacteristic.GetDescriptor(UUID_SUBSCRIPTION_DESCRIPTOR);

                                if (null != descriptor)
                                {
                                    var manualResetEventSlim = new ManualResetEventSlim();
                                    var enable = GetEnableDescriptorValue();

                                    stage = SequenceStage.WriteErrorStateDescriptor;
                                    waiters.Push(manualResetEventSlim);

                                    var result = descriptor.SetValue(enable) && bluetoothGatt!.WriteDescriptor(descriptor);

                                    if (result)
                                    {
                                        ;
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Failed set/write descriptor");
                                        stage = SequenceStage.Failed;
                                    }

                                    if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                                    {
                                        stage = SequenceStage.GetRpcResultCharacteristic;
                                    }
                                    else
                                    {
                                        Debug.WriteLine("set/write descriptor timed out");
                                        stage = SequenceStage.Failed;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("No subscription descriptor");
                                    stage = SequenceStage.Failed;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Failed set notification");
                                stage = SequenceStage.Failed;
                            }
                        }

                        break;
                    }

                    case SequenceStage.GetRpcResultCharacteristic:
                    {
                        if (null != provisionService)
                        {
                            var characteristic = provisionService.GetCharacteristic(UUID_CHARACTERISTIC_RPC_RESULT);

                            if (null != characteristic)
                            {
                                stage = SequenceStage.SubscribeRpcResultCharacteristic;
                                rpcResultCharacteristic = characteristic;
                            }
                            else
                            {
                                Debug.WriteLine("No Error code characteristic");
                                stage = SequenceStage.Failed;
                            }
                        }

                        break;
                    }

                    case SequenceStage.SubscribeRpcResultCharacteristic:
                    {
                        if (null != rpcResultCharacteristic)
                        {
                            var success =
                                bluetoothGatt!.SetCharacteristicNotification(rpcResultCharacteristic, true);

                            if (success)
                            {
                                var descriptor = rpcResultCharacteristic.GetDescriptor(UUID_SUBSCRIPTION_DESCRIPTOR);

                                if (null != descriptor)
                                {
                                    var manualResetEventSlim = new ManualResetEventSlim();
                                    var enable = GetEnableDescriptorValue();

                                    stage = SequenceStage.WriteRpcResultDescriptor;
                                    waiters.Push(manualResetEventSlim);

                                    var result = descriptor.SetValue(enable) && bluetoothGatt!.WriteDescriptor(descriptor);

                                    if (result)
                                    {
                                        ;
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Failed set/write descriptor");
                                        stage = SequenceStage.Failed;
                                    }

                                    if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                                    {
                                        stage = SequenceStage.GetRpcCommandCharacteristic;
                                    }
                                    else
                                    {
                                        Debug.WriteLine("set/write descriptor timed out");
                                        stage = SequenceStage.Failed;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("No subscription descriptor");
                                    stage = SequenceStage.Failed;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Failed set notification");
                                stage = SequenceStage.Failed;
                            }
                        }

                        break;
                    }

                    case SequenceStage.GetRpcCommandCharacteristic:
                    {
                        if (null != provisionService)
                        {
                            var characteristic = provisionService.GetCharacteristic(UUID_CHARACTERISTIC_RPC);

                            if (null != characteristic)
                            {
                                stage = SequenceStage.Complete;
                                rpcCommandCharacteristic = characteristic;
                            }
                            else
                            {
                                Debug.WriteLine("No RPC command characteristic");
                                stage = SequenceStage.Failed;
                            }
                        }

                        break;
                    }

                    case SequenceStage.GetCapabilitiesCharacteristic:
                    {
                        if (null != provisionService)
                        {
                            var characteristic = provisionService.GetCharacteristic(UUID_CHARACTERISTIC_CAPABILITIES);

                            if (null != characteristic)
                            {
                                var manualResetEventSlim = new ManualResetEventSlim();

                                stage = SequenceStage.ReadCapabilitiesCharacteristic;
                                waiters.Push(manualResetEventSlim);

                                capabilitiesCharacteristic = characteristic;

                                var success = bluetoothGatt!.ReadCharacteristic(capabilitiesCharacteristic);

                                if (success)
                                {
                                    if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                                    {
                                        var value = capabilitiesCharacteristic.GetIntValue(GattFormat.Sint8, 0);

                                        if (null != value)
                                        {
                                            var deviceCapabilities = value.ByteValue();

                                            Debug.WriteLine($"Device capabilities: 0x{deviceCapabilities:X2}");

                                            stage = SequenceStage.GetCurrentStateCharacteristic;
                                        }
                                        else
                                        {
                                            Debug.WriteLine("Read Error code invalid format");
                                            stage = SequenceStage.Failed;
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Read Error code timed out");
                                        stage = SequenceStage.Failed;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("Read Error code failed");
                                    stage = SequenceStage.Failed;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("No Error code characteristic");
                                stage = SequenceStage.Failed;
                            }
                        }

                        break;
                    }
                }
            }

            if (SequenceStage.Failed == stage)
            {
                ;
            }

            callback.Invoke(SequenceStage.Complete == stage);
        }

        private static byte[] GetEnableDescriptorValue()
        {
            return BluetoothGattDescriptor.EnableNotificationValue?.ToArray() ?? new byte[] { 0x00, 0x00 };
        }

        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            switch (stage)
            {
                case SequenceStage.BluetoothGattConnecting:
                {
                    if (GattStatus.Success == status)
                    {
                        if (ProfileState.Connected == newState)
                        {
                            bluetoothGatt = gatt;

                            if (waiters.TryPop(out var handle))
                            {
                                handle.Set();
                            }
                            else
                            {
                                Debug.WriteLine("Nothing in queue");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"ProfileState: {newState}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"GattStatus: {status}");
                    }

                    break;
                }
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);

            switch (stage)
            {
                case SequenceStage.DiscoveringServices:
                {
                    if (GattStatus.Success == status)
                    {
                        var service = gatt?.GetService(UUID_SERVICE_PROVISION);

                        if (null != service)
                        {
                            provisionService = service;

                            if (waiters.TryPop(out var handle))
                            {
                                handle.Set();
                            }
                            else
                            {
                                Debug.WriteLine("Nothing in queue");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Can't get provision service");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Service discovering failed");
                    }

                    break;
                }
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            base.OnCharacteristicRead(gatt, characteristic, status);

            switch (stage)
            {
                case SequenceStage.ReadCurrentStateCharacteristic:
                {
                    if (GattStatus.Success == status)
                    {
                        if (UUID_CHARACTERISTIC_CURRENT_STATE?.Equals(characteristic?.Uuid) ?? false)
                        {
                            if (waiters.TryPop(out var handler))
                            {
                                handler.Set();
                            }
                            else
                            {
                                Debug.WriteLine("Nothing in stack");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Wrong descriptor");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Bad status");
                    }

                    break;
                }

                case SequenceStage.ReadErrorStateCharacteristic:
                {
                    if (GattStatus.Success == status)
                    {
                        if (UUID_CHARACTERISTIC_ERROR_STATE?.Equals(characteristic?.Uuid) ?? false)
                        {
                            if (waiters.TryPop(out var handler))
                            {
                                handler.Set();
                            }
                            else
                            {
                                Debug.WriteLine("Nothing in stack");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Wrong descriptor");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Bad status");
                    }

                    break;
                }

                case SequenceStage.ReadRpcResultCharacteristic:
                {
                    if (GattStatus.Success == status)
                    {
                        if (UUID_CHARACTERISTIC_RPC_RESULT?.Equals(characteristic?.Uuid) ?? false)
                        {
                            if (waiters.TryPop(out var handler))
                            {
                                handler.Set();
                            }
                            else
                            {
                                Debug.WriteLine("Nothing in stack");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Wrong descriptor");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Bad status");
                    }

                    break;
                }

                case SequenceStage.ReadCapabilitiesCharacteristic:
                {
                    if (GattStatus.Success == status)
                    {
                        if (UUID_CHARACTERISTIC_CAPABILITIES?.Equals(characteristic?.Uuid) ?? false)
                        {
                            if (waiters.TryPop(out var handler))
                            {
                                handler.Set();
                            }
                            else
                            {
                                Debug.WriteLine("Nothing in stack");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Wrong descriptor");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Bad status");
                    }

                    break;
                }
            }
        }

        public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        {
            base.OnDescriptorWrite(gatt, descriptor, status);

            switch (stage)
            {
                case SequenceStage.WriteCurrentStateDescriptor:
                case SequenceStage.WriteErrorStateDescriptor:
                case SequenceStage.WriteRpcResultDescriptor:
                {
                    if (GattStatus.Success == status)
                    {
                        if (UUID_SUBSCRIPTION_DESCRIPTOR?.Equals(descriptor?.Uuid) ?? false)
                        {
                            if (waiters.TryPop(out var handler))
                            {
                                handler.Set();
                            }
                            else
                            {
                                Debug.WriteLine("Nothing in stack");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Unknown descriptor");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Wrong status: {status}");
                    }

                    break;
                }

                default:
                {
                    break;
                }
            }
        }

        private enum SequenceStage
        {
            Failed = -1,
            NotStarted,
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
            Complete
        }
    }
}

#nullable restore