
#nullable enable

using Android.Bluetooth;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SmartPot.Application.Core
{
    partial class ImprovDevice
    {
        private void ConnectSequence()
        {
            bool CanProceed() => SequenceStage.Connected != stage && SequenceStage.Failed != stage;

            while (CanProceed())
            {
                switch (stage)
                {
                    case SequenceStage.Disconnected:
                    {
                        var manualResetEventSlim = new ManualResetEventSlim();

                        stage = SequenceStage.BluetoothGattConnecting;
                        waiters.Push(manualResetEventSlim);

                        var gatt = device.ConnectGatt(context, true, bluetoothCallback);

                        if (null != gatt)
                        {
                            Debug.WriteLine("Start getting BluetoothGatt");
                        }

                        if (manualResetEventSlim.Wait(TimeSpan.FromSeconds(30.0d)))
                        {
                            stage = SequenceStage.BluetoothGattAcquired;
                        }
                        else
                        {
                            Debug.WriteLine("Get BluetoothGatt timed out");
                            stage = SequenceStage.Failed;
                        }

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
                                            DeviceState = (DeviceState)value.ByteValue();

                                            Debug.WriteLine($"Read Current state: {DeviceState}");

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
                            var success =
                                bluetoothGatt!.SetCharacteristicNotification(currentStateCharacteristic, true);

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
                                            ErrorCode = (ErrorCode)value.ByteValue();

                                            Debug.WriteLine($"Read Error code: {ErrorCode}");

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
                            var success = bluetoothGatt!.SetCharacteristicNotification(rpcResultCharacteristic, true);

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
                                stage = SequenceStage.Connected;
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
                }
            }

            if (SequenceStage.Connected != stage)
            {
                ;
            }

            ;
        }

        private static byte[] GetEnableDescriptorValue()
        {
            return BluetoothGattDescriptor.EnableNotificationValue?.ToArray() ?? new byte[] { 0x00, 0x00 };
        }
    }
}

#nullable restore
