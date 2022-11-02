using System;
using System.Collections.Generic;
using Android.Bluetooth;
using Android.Bluetooth.LE;

namespace SmartPot.Application.Core
{
    internal partial class ImprovManager
    {
        private sealed class BluetoothScanCallback : ScanCallback
        {
            private readonly SmartPot.Application.Core.ImprovManager manager;

            public BluetoothScanCallback(SmartPot.Application.Core.ImprovManager manager)
            {
                this.manager = manager;
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
            {
                OnDeviceDiscovered(result?.Device);
            }

            public override void OnBatchScanResults(IList<ScanResult>? results)
            {
                for (var index = 0; null != results && index < results.Count; index++)
                {
                    var device = results[index].Device;
                    OnDeviceDiscovered(device);
                }

                base.OnBatchScanResults(results);
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
                if (ScanFailure.InternalError == errorCode)
                {
                    ;
                }

                base.OnScanFailed(errorCode);
            }
            
            private void OnDeviceDiscovered(BluetoothDevice? device)
            {
                if (null != device && false == String.IsNullOrEmpty(device.Address))
                {
                    manager.OnDeviceFound(device);
                }
            }
        }
    }
}