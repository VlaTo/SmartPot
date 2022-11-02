#nullable enable

using Android.Bluetooth;

namespace SmartPot.Application.Core
{
    internal partial class ImprovManager
    {
        private sealed class BluetoothDeviceCallback : BluetoothGattCallback
        {
            private readonly ImprovManager improvManager;

            public BluetoothDeviceCallback(ImprovManager improvManager)
            {
                this.improvManager = improvManager;
            }

            public override void OnConnectionStateChange(
                BluetoothGatt? gatt,
                GattStatus status,
                ProfileState newState)
            {
                base.OnConnectionStateChange(gatt, status, newState);

                if (GattStatus.Success == status)
                {
                    if (ProfileState.Connected == newState)
                    {
                        if (null != gatt)
                        {
                            //improvManager.DoDeviceConnected(gatt);
                        }
                        else
                        {
                            ;
                        }
                    }
                    else if (ProfileState.Disconnected == newState)
                    {
                        improvManager.DoDeviceDisconnected(gatt);
                        gatt?.Close();
                    }
                }
                else
                {
                    improvManager.DoDeviceDisconnected(gatt);
                    gatt?.Close();
                }
            }

            public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
            {
                base.OnServicesDiscovered(gatt, status);

                if (GattStatus.Success == status)
                {
                    //improvManager.DoServicesDiscovered(gatt!);
                }
            }

            public override void OnCharacteristicRead(
                BluetoothGatt? gatt,
                BluetoothGattCharacteristic? characteristic,
                GattStatus status)
            {
                base.OnCharacteristicRead(gatt, characteristic, status);

                if (GattStatus.Success == status)
                {
                    //improvManager.DoCharacteristicRead(improvManager, gatt!, characteristic!);
                }
                else
                {
                    ;
                }
            }
        }
    }
}

#nullable restore