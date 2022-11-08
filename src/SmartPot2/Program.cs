using System;
using System.Device.I2c;
using System.Device.Wifi;
using System.Diagnostics;
using System.Threading;
using Iot.Device.Rtc;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Networking;
using nanoFramework.Runtime.Native;
using SmartPot2.Core;
using SmartPot2.Devices;

namespace SmartPot2
{
    public class Program
    {
        const string ssid = "xxxxxxxxx";
        const string password = "*********";
        
        private static Ds3231 rtc;
        private static At24Cxx eeprom;

        public static void Main()
        {
            // Setup I2C bus#1 pins
            Configuration.SetPinFunction(Gpio.IO21, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(Gpio.IO22, DeviceFunction.I2C1_CLOCK);

            const int i2cBusId = 1;

            rtc = new Ds3231(
                I2cDevice.Create(new I2cConnectionSettings(i2cBusId, Ds3231.DefaultI2cAddress))
            );
            eeprom = new At24Cxx(
                I2cDevice.Create(new I2cConnectionSettings(i2cBusId, At24Cxx.DefaultI2cAddress)),
                At24Cxx.Size.Rom32
            );

            var canReconnect = false;
            var deviceConnection = DeviceConnection.FromEeprom(eeprom, 0x00);

            if (null != deviceConnection)
            {
                Debug.WriteLine("Has device connection");
                canReconnect = DeviceConnection.DeviceConnectionStatus.Connected == deviceConnection.Status;
            }
            else
            {
                Debug.WriteLine("Device connection is invalid -or- absent");
            }

            Debug.WriteLine($"Reconnect: {canReconnect}");

            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60L));
            var success = canReconnect
                ? WifiNetworkHelper.Reconnect(requiresDateTime: true, token: cancellation.Token)
                : WifiNetworkHelper.ConnectDhcp(ssid, password, requiresDateTime: true, token: cancellation.Token);

            if (success)
            {
                var dateTime = DateTime.UtcNow;
                var connectionInvalidated = null == deviceConnection;

                if (null != deviceConnection)
                {
                    var elapsed = dateTime - deviceConnection.LastTimeSynchronized;

                    if (TimeSpan.FromDays(1L) < elapsed)
                    {
                        // set DS3231 UTC time
                        rtc.DateTime = dateTime;
                        connectionInvalidated = true;
                        Debug.WriteLine("Adjusting RTC clock from SNTP");
                    }
                }

                if (connectionInvalidated)
                {
                    deviceConnection = new DeviceConnection(DeviceConnection.DeviceConnectionStatus.Connected, dateTime);
                    deviceConnection.WriteTo(eeprom, 0x00);
                    Debug.WriteLine("Writing device connection");
                }

                Debug.WriteLine($"Connected to WIFI {ssid}");
            }
            else
            {
                Debug.WriteLine($"Unable to connected to WIFI {ssid}");
                // set Device RTC clock from DS3231
                Rtc.SetSystemTime(rtc.DateTime);

                if (deviceConnection is { Status: DeviceConnection.DeviceConnectionStatus.Connected })
                {
                    deviceConnection = new DeviceConnection(DeviceConnection.DeviceConnectionStatus.Failed, DateTime.MinValue);
                    deviceConnection.WriteTo(eeprom, 0x00);
                    Debug.WriteLine("Writing device connection");
                }
            }

            var localTime = DateTime.UtcNow + TimeSpan.FromHours(3L);
            Debug.WriteLine($"Date: {localTime:F}");

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
