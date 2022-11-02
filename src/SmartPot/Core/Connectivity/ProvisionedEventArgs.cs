using System;

namespace SmartPot.Core.Connectivity
{
    /// <summary>
    /// Event Args for user provisioning.
    /// </summary>
    public class ProvisionedEventArgs : EventArgs
    {
        public string Ssid
        {
            get;
        }

        public string Password
        {
            get;
        }

        public ProvisionedEventArgs(string ssid, string password)
        {
            Ssid = ssid;
            Password = password;
        }
    }
}