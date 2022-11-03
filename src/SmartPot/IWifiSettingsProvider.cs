
#nullable enable

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
    }

    internal interface IWifiSettingsProvider
    {
        WifiSettings[] GetSettings();

        void AddSettings(WifiSettings settings);

        void RemoveSettings(WifiSettings settings);

        void ClearAll();
    }
}

#nullable restore