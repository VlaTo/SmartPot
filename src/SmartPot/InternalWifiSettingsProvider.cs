
#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace SmartPot
{
    internal sealed class InternalWifiSettingsProvider : IWifiSettingsProvider
    {
        private const string InternalDrive = @"I:\";
        private const string FolderName = "Wifi";
        private const string FileName = "Settings.bin";

        public WifiSettings[] GetSettings()
        {
            if (InternalDriveExists())
            {
                var folder = Path.Combine(InternalDrive, FolderName);
                var path = Path.Combine(folder, FileName);

                if (File.Exists(path))
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        return ReadSettings(stream);
                    }
                }
            }

            return new WifiSettings[0];
        }

        public void AddSettings(WifiSettings settings)
        {
            if (InternalDriveExists())
            {
                var folder = Path.Combine(InternalDrive, FolderName);
                var path = Path.Combine(folder, FileName);

                using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    if (0 < stream.Length)
                    {
                        var position = stream.Seek(-1L, SeekOrigin.End);

                        if (position >= stream.Length)
                        {
                            ;
                        }
                    }

                    WriteBlock(stream, settings);
                    WriteEmptyBlock(stream);
                }

                return ;
            }
        }

        public bool RemoveSettings(WifiSettings settings)
        {
            if (InternalDriveExists())
            {
                var folder = Path.Combine(InternalDrive, FolderName);
                var path = Path.Combine(folder, FileName);

                if (File.Exists(path))
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        while (true)
                        {
                            var position = stream.Position;
                            var packetLength = stream.ReadByte();

                            if (0 == packetLength)
                            {
                                break;
                            }

                            var block = ReadBlock(stream, packetLength);

                            if (settings.Equals(block))
                            {
                                if (position != stream.Seek(position, SeekOrigin.Begin))
                                {
                                    throw new Exception();
                                }


                            }
                        }
                    }
                }
            }

            return false;
        }

        public void ClearAll()
        {
            throw new System.NotImplementedException();
        }

        private static bool InternalDriveExists()
        {
            var drives = Directory.GetLogicalDrives();

            for (var index = 0; index < drives.Length; index++)
            {
                if (String.Equals(drives[index], InternalDrive))
                {
                    return true;
                }
            }

            return false;
        }

        private static WifiSettings[] ReadSettings(Stream stream)
        {
            if (0 < stream.Length)
            {
                var array = new ArrayList();

                while (true)
                {
                    var packetLength = stream.ReadByte();

                    if (0 == packetLength)
                    {
                        break;
                    }

                    var settings = ReadBlock(stream, packetLength);

                    array.Add(settings);
                }

                var result = new WifiSettings[array.Count];

                for (var index = 0; index < array.Count; index++)
                {
                    result[index] = (WifiSettings)array[index];
                }

                return result;
            }

            return new WifiSettings[0];
        }

        private static WifiSettings ReadBlock(Stream stream, int packetLength)
        {
            var length = 0;
            
            length += ReadString(stream, out var ssid);
            length += ReadString(stream, out var passphrase);

            if (packetLength != length)
            {
                throw new Exception();
            }

            return new WifiSettings(ssid, passphrase);
        }

        private static int ReadString(Stream stream, out string str)
        {
            var length = stream.ReadByte();

            if (0 == length)
            {
                str = String.Empty;
                return length;
            }

            var buffer = new byte[length];
            var count = stream.Read(buffer, 0, length);

            if (length != count)
            {
                throw new Exception();
            }

            str = Encoding.UTF8.GetString(buffer, 0, count);

            return length;
        }

        private static int WriteBlock(Stream stream, WifiSettings settings)
        {
            var encoding = Encoding.UTF8;
            var ssIdBytes = encoding.GetBytes(settings.Ssid);
            var passPhraseBytes = String.IsNullOrEmpty(settings.Passphrase) ? null : encoding.GetBytes(settings.Passphrase);
            var length = passPhraseBytes != null ? passPhraseBytes.Length : 0;
            var packetLength = ssIdBytes.Length + length + sizeof(byte) * 3;

            stream.WriteByte((byte)packetLength);
            stream.WriteByte((byte)ssIdBytes.Length);
            stream.Write(ssIdBytes, 0, ssIdBytes.Length);
            stream.WriteByte((byte)length);

            if (null != passPhraseBytes)
            {
                stream.Write(passPhraseBytes, 0, passPhraseBytes.Length);
            }

            return packetLength;
        }

        private static void WriteEmptyBlock(Stream stream)
        {
            stream.WriteByte(0);
        }
    }
}

#nullable restore