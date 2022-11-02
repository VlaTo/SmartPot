using nanoFramework.Hosting;
using SmartPot.Core.Connectivity;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;

namespace SmartPot.Core.Hosted.Services
{
    internal sealed class ImprovDeviceService : BackgroundService
    {
        private readonly ImprovManager manager;

        public ImprovDeviceService(ImprovManager manager)
        {
            this.manager = manager;
        }

        protected override void ExecuteAsync()
        {
            // This optional event will be fired if asked to identify device
            // You can flash an LED or some other method.
            manager.OnIdentify += DoIdentify;

            // This optional event is called when the provisioning is completed and Wifi is connected but before
            // improv has informed Improv client of result. This allows user to set the provision URL redirect with correct IP address 
            // See event handler
            manager.OnProvisioningComplete += DoProvisioningComplete;

            // This optional event will be called to do the Wifi provisioning in user program.
            // if not set then improv class will automatically try to connect to Wifi 
            // For this sample we will let iprov do it, uncomment next line to try user event. See event handler
            //manager.OnProvisioned += DoProvisioned;

            manager.Start(GetDeviceName());

            // You may need a physical button to be pressed to authorise the provisioning (security)
            // Wait for button press and call Authorise method
            // For out test we will just Authorise
            manager.Authorize(true);

            // Now wait for Device to be Provisioned
            // we could also just use the OnProvisioningComplete event
            while (ImprovState.Provisioned != manager.CurrentState)
            {
                Thread.Sleep(100);
            }

            manager.Stop();
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

        private void DoProvisioningComplete(object sender, EventArgs e)
        {
            //SetProvisioningURL();
            Debug.WriteLine("Provisioning complete");
        }

        private void DoIdentify(object sender, EventArgs e)
        {
            // Flash LED to Identify device or do nothing
            Debug.WriteLine("Flashing LED...");
        }
    }
}