
#nullable enable

using System.Diagnostics;
using Android.Bluetooth;

namespace SmartPot.Application.Core
{
    partial class ImprovDevice
    {
        private void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
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

        private void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
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

        #region Characteristic callbacks

        private void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            if (UUID_CHARACTERISTIC_CURRENT_STATE?.Equals(characteristic?.Uuid) ?? false)
            {
                var value = characteristic?.GetIntValue(GattFormat.Sint8, 0);

                if (null != value)
                {
                    DeviceState = (DeviceState)value.ByteValue();
                    Debug.WriteLine($"Device state: {DeviceState}");
                }
                else
                {
                    Debug.WriteLine("Device state no value");
                }
            }
            else if (UUID_CHARACTERISTIC_ERROR_STATE?.Equals(characteristic?.Uuid) ?? false)
            {
                var value = characteristic?.GetIntValue(GattFormat.Sint8, 0);

                if (null != value)
                {
                    ErrorCode = (ErrorCode)value.ByteValue();
                    Debug.WriteLine($"Error code: {ErrorCode}");
                }
                else
                {
                    Debug.WriteLine("Error code no value");
                }
            }
            else if (UUID_CHARACTERISTIC_RPC_RESULT?.Equals(characteristic?.Uuid) ?? false)
            {
                var value = characteristic?.GetValue();

                if (null != value)
                {
                    var length = value.Length;
                    Debug.WriteLine($"RPC result length: {length}");

                    if (waiters.TryPop(out var handler))
                    {
                        handler.Set();
                    }
                    else
                    {
                        Debug.WriteLine("No waiter in stack");
                    }
                }
                else
                {
                    Debug.WriteLine("RPC result no value");
                }
            }
            else
            {
                Debug.WriteLine("Unknown characteristic changed");
            }
        }

        private void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
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

        private void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            switch (stage)
            {
                case SequenceStage.SendRpcRequest:
                {
                    if (UUID_CHARACTERISTIC_RPC?.Equals(characteristic?.Uuid) ?? false)
                    {
                        if (GattStatus.Success == status)
                        {
                            if (waiters.TryPop(out var handler))
                            {
                                handler.Set();
                            }
                            else
                            {
                                ;
                            }
                        }
                        else
                        {
                            ;
                        }
                    }
                    else
                    {
                        ;
                    }

                    break;
                }
            }
        }

        #endregion

        #region Descriptor callbacks

        private void OnDescriptorRead(BluetoothGatt? gatt, BluetoothGattDescriptor? characteristic, GattStatus status)
        {
        }

        private void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        {
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

        #endregion
    }
}

#nullable restore
