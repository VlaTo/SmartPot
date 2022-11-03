
#nullable enable

using System;
using System.Collections.Generic;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Java.Util;
using static Android.Bluetooth.BluetoothClass;
using ScanMode = Android.Bluetooth.LE.ScanMode;

namespace SmartPot.Application.Core
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class FoundDeviceEventArgs : EventArgs
    {
        public ImprovDevice Device
        {
            get;
        }

        public FoundDeviceEventArgs(ImprovDevice device)
        {
            Device = device;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class ScanFailedEventArgs : EventArgs
    {
        public ScanFailure Failure
        {
            get;
        }

        public ScanFailedEventArgs(ScanFailure failure)
        {
            Failure = failure;
        }
    }

    // https://github.com/improv-wifi/sdk-android/blob/main/library/src/main/java/com/wifi/improv/ImprovManager.kt
    internal sealed partial class ImprovManager : Java.Lang.Object
    {
        private static readonly UUID? UUID_CHARACTERISTIC_CAPABILITIES =  UUID.FromString("00467768-6228-2272-4663-277478268005");

        private readonly Context context;
        private readonly BluetoothScanCallback scanCallback;
        private readonly List<ImprovDevice> discoveredDevices;
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

        public event EventHandler<FoundDeviceEventArgs>? FoundDevice;
        
        public event EventHandler<ScanFailedEventArgs>? ScanFailed;

        public event EventHandler? ScanStateChanged;

        public ImprovManager(Context context)
        {
            this.context = context;

            workItemQueue = new WorkItemQueue();
            discoveredDevices = new List<ImprovDevice>();
            scanCallback = new BluetoothScanCallback
            {
                ScanResult = OnScanResult,
                ScanFailed = OnScanFailed
            };
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
                RaiseScanStateChangedEvent(EventArgs.Empty);

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

                RaiseScanStateChangedEvent(EventArgs.Empty);
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

        private void RaiseScanStateChangedEvent(EventArgs e)
        {
            var handlers = ScanStateChanged;

            if (null != handlers)
            {
                handlers.Invoke(this, e);
            }
        }

        private void RaiseFoundDeviceEvent(FoundDeviceEventArgs e)
        {
            var handlers = FoundDevice;

            if (null != handlers)
            {
                handlers.Invoke(this, e);
            }
        }

        private void RaiseScanFailedEvent(ScanFailedEventArgs e)
        {
            var handlers = ScanFailed;

            if (null != handlers)
            {
                handlers.Invoke(this, e);
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

        private void OnScanResult(BluetoothDevice discoveredDevice)
        {

            bool IsSame(ImprovDevice improvDevice, BluetoothDevice bluetoothDevice)
            {
                return String.Equals(improvDevice.Address, bluetoothDevice.Address);
            }

            if (discoveredDevices.Exists(device => IsSame(device, discoveredDevice)))
            {
                return;
            }

            var improvDevice = new ImprovDevice(this, context, workItemQueue, discoveredDevice);

            discoveredDevices.Add(improvDevice);
            RaiseFoundDeviceEvent(new FoundDeviceEventArgs(improvDevice));
        }

        private void OnScanFailed(ScanFailure failure)
        {
            RaiseScanFailedEvent(new ScanFailedEventArgs(failure));
        }
    }
}

#nullable restore