
#nullable enable

using System.Collections;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SmartPot
{
    internal sealed class InternalWifiSettingsProvider : IWifiSettingsProvider
    {
        private const string FolderName = "Wifi";
        private const string FileName = "Settings.bin";

        private StorageFolder? folder;
        private StorageFile? file;

        public InternalWifiSettingsProvider()
        {
        }

        public void Initialize()
        {
            folder = KnownFolders.InternalDevices.CreateFolder(FolderName, CreationCollisionOption.OpenIfExists);
            file = folder.CreateFile(FileName, CreationCollisionOption.OpenIfExists);
        }

        public WifiSettings[] GetSettings()
        {
            var result = new ArrayList();
            var buffer = FileIO.ReadBuffer(file);

            //new InMemoryRandomAccessStream()
            using (var reader = DataReader.FromBuffer(buffer))
            {
                while (true)
                {
                    var length = reader.ReadByte();

                    if (1 > length)
                    {
                        break;
                    }

                    result.Add(new WifiSettings("", null));
                }
            }

            var temp = new WifiSettings[result.Count];

            return temp;
        }

        public void AddSettings(WifiSettings settings)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSettings(WifiSettings settings)
        {
            throw new System.NotImplementedException();
        }

        public void ClearAll()
        {
            throw new System.NotImplementedException();
        }
    }
}

#nullable restore