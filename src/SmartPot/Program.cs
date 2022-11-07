using Iot.Device.Rtc;
using Iot.Device.Ssd13xx;
using nanoFramework.Hardware.Esp32;
using SmartPot.Core;
using SmartPot.Core.Connectivity;
using SmartPot.Core.Devices;
using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;

namespace SmartPot
{
    // 0x3C - OLED
    // 0x57 - AT24C32
    // 0x68 - DS3231
    public class Program
    {
        private static GpioController gpioController;
        private static Display display;
        private static Ds3231 rtc;
        private static At24Cxx eeprom;
        private static ImprovManager improvManager;

        public static void Main()
        {
            // Setup I2C bus#1 pins
            Configuration.SetPinFunction(Gpio.IO21, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(Gpio.IO22, DeviceFunction.I2C1_CLOCK);

            //Sleep.EnableWakeupByPin(Sleep.WakeupGpioPin.Pin0, 0);
            //Sleep.StartDeepSleep();

            gpioController = new GpioController();
            
            var led = gpioController.OpenPin(Gpio.IO05, PinMode.Output);

            const int busId = 1;
            
            display = CreateDisplayDevice(busId);
            rtc = CreateRtcDevice(busId);
            eeprom = CreateEepromDevice(busId);

            display.ClearScreen();

            var bitmap = Resource.GetBytes(Resource.BinaryResources.logo);

            display.DrawBitmap(0, 0, 16, 64, bitmap);
            display.Display();

            if (ValidateEepromCrc(eeprom, out var newCrc))
            {
                Debug.WriteLine("EEPROM CRC failed!");
                //eeprom.Write(0x00, new[] { newCrc });
            }

            // Display setup
            display.Font = new BasicFont();

            // Creating ImprovManager
            improvManager = new ImprovManager();

            // This optional event will be fired if asked to identify device
            // You can flash an LED or some other method.
            improvManager.OnIdentify += DoIdentify;

            // This optional event is called when the provisioning is completed and Wifi is connected but before
            // improv has informed Improv client of result. This allows user to set the provision URL redirect with correct IP address 
            // See event handler
            improvManager.OnProvisioningComplete += DoProvisioningComplete;

            // This optional event will be called to do the Wifi provisioning in user program.
            // if not set then improv class will automatically try to connect to Wifi 
            // For this sample we will let iprov do it, uncomment next line to try user event. See event handler
            //manager.OnProvisioned += DoProvisioned;

            var cancellation = new CancellationTokenSource();
            var blink = new LedBlink(led, cancellation.Token);
            var thread = new Thread(blink.Run);

            improvManager.Start(GetDeviceName());

            // You may need a physical button to be pressed to authorise the provisioning (security)
            // Wait for button press and call Authorise method
            // For out test we will just Authorise
            improvManager.Authorize(true);

            // Now wait for Device to be Provisioned
            // we could also just use the OnProvisioningComplete event

            thread.Start();

            while (ImprovState.Provisioned != improvManager.CurrentState)
            {
                Thread.Sleep(100);
            }

            improvManager.Stop();

            cancellation.Cancel();
            thread.Join();
            
            /*var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    // devices
                    services.AddSingleton(typeof(RtcBase), rtc);
                    services.AddSingleton(typeof(Ssd1306), display);
                    services.AddSingleton(typeof(At24Cxx), eeprom);
                    // services
                    services.AddSingleton(typeof(ImprovManager));
                    services.AddSingleton(typeof(IDateTimeProvider), typeof(RtcDateTimeProvider));
                    services.AddSingleton(typeof(UserController));
                    services.AddTransient(typeof(ImprovManager));
                    // hosted services
                    //services.AddHostedService(typeof(UserControllerService));
                    services.AddHostedService(typeof(ImprovDeviceService));
                })
                .Build();

            host.Run();*/

            /*Debug.WriteLine("Example of using IMPROV bluetooth LE for Wifi provisioning");

            // Construct Improv class
            _imp = new ImprovManager();

            // This optional event will be fired if asked to identify device
            // You can flash an LED or some other method.
            _imp.OnIdentify += Imp_OnIdentify;

            // This optional event is called when the provisioning is completed and Wifi is connected but before
            // improv has informed Improv client of result. This allows user to set the provision URL redirect with correct IP address 
            // See event handler
            _imp.OnProvisioningComplete += Imp_OnProvisioningComplete;

            // This optional event will be called to do the Wifi provisioning in user program.
            // if not set then improv class will automatically try to connect to Wifi 
            // For this sample we will let iprov do it, uncomment next line to try user event. See event handler
            _imp.OnProvisioned += Imp_OnProvisioned;

            // Start IMPROV service to start advertising using provided device name.
            var deviceId = GetDeviceId();
            var deviceName = $"SmartPot-{deviceId:X}";
            
            //display.DrawString(1, 1, "Привет");
            display.DrawString(1, 1, deviceName);
            display.Display();

            _imp.Start(deviceName);

            // You may need a physical button to be pressed to authorise the provisioning (security)
            // Wait for button press and call Authorise method
            // For out test we will just Authorise
            _imp.Authorize(true);

            display.DrawString(1, 1, "Waiting for device");
            display.DrawString(1, display.Font.Height, "to be provisioned");
            display.Display();

            // Now wait for Device to be Provisioned
            // we could also just use the OnProvisioningComplete event
            while (_imp.CurrentState != ImprovState.Provisioned)
            {
                Thread.Sleep(500);
            }

            display.ClearScreen();
            display.DrawString(1, 1, "Device has been");
            display.DrawString(1, display.Font.Height, "provisioned");
            display.Display();

            var ipAddress = _imp.GetCurrentIPAddress();

            display.DrawString(1, display.Font.Height * 2, $"IP: {ipAddress}");
            display.Display();

            // We are now provisioned and connected to Wifi, so stop bluetooth service to release resources.
            _imp.Stop();
            _imp = null;
            
            Thread.Sleep(3000);

            display.ClearScreen();
            display.DrawString(1, 1, "Getting time");
            display.Display();

            Sntp.Start();
            Sntp.UpdateNow();

            display.ClearScreen();

            do
            {
                var dateTimeUtc = DateTime.UtcNow + TimeSpan.FromHours(3L);
                var date = $"{dateTimeUtc:d}";
                var time = $"{dateTimeUtc:T}";

                display.DrawString(1, 1, date);
                display.DrawString(1, display.Font.Height + 3, time);
                display.Display();

                Thread.Sleep(1000);

            } while (true);
            */

            // Start our very simple web page server to pick up the redirect we gave
            //Debug.WriteLine("Starting simple web server");
            //SimpleWebListener();

            Thread.Sleep(Timeout.Infinite);
        }

        private static Display CreateDisplayDevice(int busId)
        {
            var settings = new I2cConnectionSettings(busId, Ssd1306.DefaultI2cAddress, I2cBusSpeed.FastMode);
            return new Display(I2cDevice.Create(settings), Ssd13xx.DisplayResolution.OLED128x64);
        }

        private static Ds3231 CreateRtcDevice(int busId)
        {
            var settings = new I2cConnectionSettings(busId, Ds3231.DefaultI2cAddress);
            return new Ds3231(I2cDevice.Create(settings));
        }

        private static At24Cxx CreateEepromDevice(int busId)
        {
            var settings = new I2cConnectionSettings(busId, At24Cxx.DefaultI2cAddress);
            return new At24Cxx(I2cDevice.Create(settings), At24Cxx.Size.Rom32);
        }

        private static bool ValidateEepromCrc(At24Cxx memory, out byte newCrc)
        {
            const ushort offset = sizeof(byte);
            using var crc = new Crc8();
            var numOfBytes = memory.GetMaxLength() - offset;
            var actualCrc = ReadEepromCrc(memory);
            var bytes = memory.Read(offset, numOfBytes);
                
            newCrc = crc.Compute(bytes);

            return actualCrc == newCrc;
        }

        private static byte ReadEepromCrc(At24Cxx memory)
        {
            var bytes = memory.Read(0x00, 1);
            return bytes[0];
        }
        
        private static string GetDeviceName()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            if (0 < networkInterfaces.Length)
            {
                using var crc = new Crc8();
                var num = crc.Compute(networkInterfaces[0].PhysicalAddress);
                return $"SmartPot-{num:X2}";
            }

            return "SmartPot";
        }
        
        private static void DoProvisioningComplete(object sender, EventArgs e)
        {
            //SetProvisioningURL();
            Debug.WriteLine("Provisioning complete");
        }

        private static void DoIdentify(object sender, EventArgs e)
        {
            // Flash LED to Identify device or do nothing
            Debug.WriteLine("Flashing LED...");
        }

        /// <summary>
        /// Event handler for OnProvisioningComplete event
        /// </summary>
        /// <param name="sender">Improv instance</param>
        /// <param name="e">Not used</param>
        /*private static void Imp_OnProvisioningComplete(object sender, EventArgs e)
        {
            SetProvisioningURL();
        }*/

        /// <summary>
        /// Set URL with current IP address
        /// The Improv client will redirect to this URL if set.
        /// </summary>
        /*private static void SetProvisioningURL()
        {
            // All good, wifi connected, set up URL for access
            _imp.RedirectUrl = "http://" + _imp.GetCurrentIPAddress() + "/start.htm";
        }*/

        /*private static void Imp_OnProvisioned(object sender, ProvisionedEventArgs e)
        {
            string ssid = e.Ssid;
            string password = e.Password;

            Debug.WriteLine("Provisioning device");

            Debug.WriteLine("Connecting to Wifi...");

            // Try to connect to Wifi AP
            // use improv internal method
            if (_imp.ConnectWiFi(ssid, password))
            {
                Debug.WriteLine("Connected to Wifi");

                SetProvisioningURL();
            }
            else
            {
                Debug.WriteLine("Failed to Connect to Wifi!");

                // if not successful set error and return
                _imp.ErrorState = ImprovError.UnableConnect;
            }
        }*/

        /*private static void Imp_OnIdentify(object sender, EventArgs e)
        {
            // Flash LED to Identify device or do nothing
            Debug.WriteLine("Flashing LED...");
        }*/

        /*private static void SimpleWebListener()
        {
            // set-up our HTTP response
            string responseString =
                "<HTML><BODY>" +
                "<h2>Hello from nanoFramework</h2>" +
                "<p>We are a newly provisioned device using <b>Improv</b> over Bluetooth.</p>" +
                "<p>See <a href='https://www.improv-wifi.com'>Improv web site</a> for details" +
                "</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            // Create a listener.
            HttpListener listener = new("http", 80);

            listener.Start();

            while (true)
            {
                try
                {
                    // Now wait on context for a connection
                    HttpListenerContext context = listener.GetContext();

                    Debug.WriteLine("Web request received");

                    // Get the response stream
                    HttpListenerResponse response = context.Response;

                    // Write reply
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);

                    // output stream must be closed
                    context.Response.Close();

                    Debug.WriteLine("Web response sent");

                    // context must be closed
                    context.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("* Error getting context: " + ex.Message + "\r\nSack = " + ex.StackTrace);
                }
            }
        }*/

        /*private static int GetDeviceId()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            for (var index = 0; index < networkInterfaces.Length; index++)
            {
                var networkInterface = networkInterfaces[index];
                var macAddress = networkInterface.PhysicalAddress;
                return macAddress.GetHashCode();
            }

            return -1;
        }*/
    }
}
