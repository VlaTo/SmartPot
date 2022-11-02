
#nullable enable

using Android.Bluetooth;
using System;

namespace SmartPot.Application.Core
{
    partial class ImprovDevice
    {
        private sealed class BluetoothCallback : BluetoothGattCallback
        {
            public Action<BluetoothGatt?, GattStatus, ProfileState>? ConnectionStateChange
            {
                get;
                set;
            }

            public Action<BluetoothGatt?, GattStatus>? ServicesDiscovered
            {
                get;
                set;
            }

            public Action<BluetoothGatt?, BluetoothGattCharacteristic?, GattStatus>? CharacteristicRead
            {
                get;
                set;
            }

            public Action<BluetoothGatt?, BluetoothGattCharacteristic?, GattStatus>? CharacteristicWrite
            {
                get;
                set;
            }

            public Action<BluetoothGatt?, BluetoothGattCharacteristic?>? CharacteristicChanged
            {
                get;
                set;
            }

            public Action<BluetoothGatt?, BluetoothGattDescriptor?, GattStatus>? DescriptorRead
            {
                get;
                set;
            }

            public Action<BluetoothGatt?, BluetoothGattDescriptor?, GattStatus>? DescriptorWrite
            {
                get;
                set;
            }

            public BluetoothCallback()
            {
                ConnectionStateChange = Stub.Nop;
                ServicesDiscovered = Stub.Nop;
                CharacteristicRead = Stub.Nop;
                CharacteristicWrite = Stub.Nop;
                CharacteristicChanged = Stub.Nop;
                DescriptorRead = Stub.Nop;
                DescriptorWrite = Stub.Nop;
            }

            public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
            {
                var action = ConnectionStateChange;

                base.OnConnectionStateChange(gatt, status, newState);

                if (null != action)
                {
                    action.Invoke(gatt, status, newState);
                }
            }

            public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
            {
                var action = ServicesDiscovered;

                base.OnServicesDiscovered(gatt, status);

                if (null != action)
                {
                    action.Invoke(gatt, status);
                }
            }

            public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
            {
                var action = CharacteristicRead;

                base.OnCharacteristicRead(gatt, characteristic, status);

                if (null != action)
                {
                    action.Invoke(gatt, characteristic, status);
                }
            }

            public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
            {
                var action = CharacteristicWrite;

                base.OnCharacteristicWrite(gatt, characteristic, status);

                if (null != action)
                {
                    action.Invoke(gatt, characteristic, status);
                }
            }

            public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
            {
                var action = CharacteristicChanged;

                base.OnCharacteristicChanged(gatt, characteristic);

                if (null != action)
                {
                    action.Invoke(gatt, characteristic);
                }
            }

            public override void OnDescriptorRead(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
            {
                var action = DescriptorRead;

                base.OnDescriptorRead(gatt, descriptor, status);

                if (null != action)
                {
                    action.Invoke(gatt, descriptor, status);
                }
            }

            public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
            {
                var action = DescriptorWrite;

                base.OnDescriptorWrite(gatt, descriptor, status);

                if (null != action)
                {
                    action.Invoke(gatt, descriptor, status);
                }
            }
        }
    }
}

#nullable restore