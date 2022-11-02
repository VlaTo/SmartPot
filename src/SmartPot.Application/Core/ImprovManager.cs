
#nullable enable

using System;
using System.Collections.Generic;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Java.Util;
using ScanMode = Android.Bluetooth.LE.ScanMode;

namespace SmartPot.Application.Core
{
    // https://github.com/improv-wifi/sdk-android/blob/main/library/src/main/java/com/wifi/improv/ImprovManager.kt
    internal sealed partial class ImprovManager : Java.Lang.Object
    {
        private static readonly UUID? UUID_CHARACTERISTIC_CAPABILITIES =  UUID.FromString("00467768-6228-2272-4663-277478268005");

        private readonly Context context;
        private readonly BluetoothScanCallback scanCallback;
        private readonly List<ImprovDevice> discoveredDevices;
        private readonly List<ICallback> callbacks;
        private readonly WorkItemQueue workItemQueue;
        private bool bluetoothManagerAcquired;
        private BluetoothManager? bluetoothManager;

        public bool IsScanning
        {
            get;
            private set;
        }

        public BluetoothManager? BluetoothManager
        {
            get
            {
                if (false == bluetoothManagerAcquired)
                {
                    bluetoothManager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
                    bluetoothManagerAcquired = true;
                }

                return bluetoothManager;
            }
        }

        public BluetoothLeScanner? Scanner => BluetoothManager?.Adapter?.BluetoothLeScanner;

        public ImprovDevice[] Devices => discoveredDevices.ToArray();

        #region ICallback

        public interface ICallback
        {
            void OnScanningStateChanged(bool scanning);

            void OnDeviceFound(ImprovDevice device);
        }

        #endregion

        public ImprovManager(Context context)
        {
            this.context = context;

            workItemQueue = new WorkItemQueue();
            discoveredDevices = new List<ImprovDevice>();
            callbacks = new List<ICallback>();
            scanCallback = new BluetoothScanCallback(this);
        }

        public void AddCallback(ICallback value)
        {
            if (callbacks.Contains(value))
            {
                return;
            }

            callbacks.Add(value);
        }

        public void RemoveCallback(ICallback value)
        {
            if (callbacks.Remove(value))
            {
                ;
            }
        }

        public void FindDevices()
        {
            if (IsScanning)
            {
                return;
            }

            var scanner = Scanner;

            discoveredDevices.Clear();

            if (null != scanner)
            {
                var settings = BuildScanSettings();

                IsScanning = true;
                RaiseOnScanningStateChanged();
                scanner.StartScan(Array.Empty<ScanFilter>(), settings, scanCallback);
            }
        }

        public void StopScanning()
        {
            if (false == IsScanning)
            {
                return;
            }

            var scanner = Scanner;

            if (null != scanner)
            {
                IsScanning = false;
                scanner.StopScan(scanCallback);
                RaiseOnScanningStateChanged();
            }
        }

        /*public void IdentifyDevice()
        {
            System.Diagnostics.Debug.WriteLine("ImprovManager: Identify device");

            if (null == bluetoothGatt)
            {
                return;
            }

            var service = bluetoothGatt.GetService(UUID_SERVICE_PROVISION);
            var rpcCharacteristic = service?.GetCharacteristic(UUID_CHARACTERISTIC_RPC);

            if (null != rpcCharacteristic)
            {
                SendRpc(rpcCharacteristic, RpcCommand.Identity, Array.Empty<byte>());
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ImprovManager: No RPC characteristic");
            }
        }*/

        private void RaiseOnScanningStateChanged()
        {
            var isScanning = IsScanning;
            var handlers = callbacks.ToArray();

            for (var index = 0; index < handlers.Length; index++)
            {
                handlers[index].OnScanningStateChanged(isScanning);
            }
        }

        private void RaiseOnDeviceFound(ImprovDevice device)
        {
            var handlers = callbacks.ToArray();

            for (var index = 0; index < handlers.Length; index++)
            {
                handlers[index].OnDeviceFound(device);
            }
        }

        #region BluetoothGatt Callbacks

        private void DoDeviceDisconnected(BluetoothGatt? gatt)
        {
            ;
        }

        #endregion

        private static ScanSettings BuildScanSettings()
        {
            var builder = new ScanSettings.Builder();

            builder.SetScanMode(ScanMode.LowLatency);
            builder.SetCallbackType(ScanCallbackType.AllMatches);
            builder.SetLegacy(true);

            return builder.Build()!;
        }

        /*private static ScanFilter BuildScanFilter()
        {
            var builder = new ScanFilter.Builder();

            builder.SetServiceUuid(new ParcelUuid(UUID_SERVICE_PROVISION), new ParcelUuid(UUID_SERVICE_MASK));
            
            return builder.Build()!;
        }*/

        private void OnDeviceFound(BluetoothDevice discoveredDevice)
        {
            if (discoveredDevices.Exists(device => device.IsSame(discoveredDevice)))
            {
                return;
            }

            var improvDevice = new ImprovDevice(this, context, workItemQueue, discoveredDevice);

            discoveredDevices.Add(improvDevice);
            RaiseOnDeviceFound(improvDevice);
        }
    }
}

#nullable restore