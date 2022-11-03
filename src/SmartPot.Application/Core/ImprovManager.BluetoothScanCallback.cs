
#nullable enable

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
            public Action<BluetoothDevice>? ScanResult
            {
                get;
                set;
            }

            public Action<ScanFailure>? ScanFailed
            {
                get;
                set;
            }

            public BluetoothScanCallback()
            {
                ScanResult = Stub.Nop;
                ScanFailed = Stub.Nop;
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
            {
                OnDeviceDiscovered(result?.Device);
            }

            public override void OnBatchScanResults(IList<ScanResult>? results)
            {
                base.OnBatchScanResults(results);

                for (var index = 0; null != results && index < results.Count; index++)
                {
                    var device = results[index].Device;
                    OnDeviceDiscovered(device);
                }
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
                var action = ScanFailed;

                base.OnScanFailed(errorCode);

                if (null != action)
                {
                    action.Invoke(errorCode);
                }
            }

            private void OnDeviceDiscovered(BluetoothDevice? device)
            {
                if (null == device || String.IsNullOrEmpty(device.Address))
                {
                    return;
                }

                var action = ScanResult;

                if (null != action)
                {
                    action.Invoke(device);
                }
            }
        }
    }
}

#nullable restore
