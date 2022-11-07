
#nullable enable

using System;

namespace SmartPot
{
    internal sealed class WifiSettings
    {
        public string Ssid
        {
            get;
        }

        public string? Passphrase
        {
            get;
        }

        public WifiSettings(string ssid, string? passphrase)
        {
            Ssid = ssid;
            Passphrase = passphrase;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is WifiSettings settings && Equals(settings);
        }

        public bool Equals(WifiSettings other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return String.Equals(this.Ssid, other.Ssid) && String.Equals(this.Passphrase, other.Passphrase);
        }
    }

    internal interface IWifiSettingsProvider
    {
        WifiSettings[] GetSettings();

        void AddSettings(WifiSettings settings);

        bool RemoveSettings(WifiSettings settings);

        void ClearAll();
    }
}

#nullable restore