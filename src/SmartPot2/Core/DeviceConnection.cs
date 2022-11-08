using SmartPot2.Devices;
using System;

#nullable enable

namespace SmartPot2.Core
{
    internal sealed class DeviceConnection
    {
        private const int PacketLength = sizeof(byte) + sizeof(sbyte) + sizeof(long);
        public enum DeviceConnectionStatus : sbyte
        {
            NoNetwork = -3,
            Unauthorized = -2,
            Failed = -1,
            Disconnected = 0,
            Connected = 1
        }

        public DeviceConnectionStatus Status
        {
            get;
        }

        public DateTime LastTimeSynchronized
        {
            get;
        }

        public DeviceConnection(DeviceConnectionStatus status, DateTime lastTimeSynchronized)
        {
            Status = status;
            LastTimeSynchronized = lastTimeSynchronized;
        }

        public static DeviceConnection? FromEeprom(At24Cxx device, ushort startAddress)
        {
            var bytes = device.Read(startAddress, PacketLength);

            using var hash = new Crc8();
            var crc = hash.Compute(bytes, 1);

            if (bytes[0] != crc)
            {
                return null;
            }

            var status = (DeviceConnectionStatus)bytes[sizeof(byte)];
            var seconds = BitConverter.ToInt64(bytes, sizeof(byte) + sizeof(sbyte));
            var dateTime = DateTime.FromUnixTimeSeconds(seconds);

            return new DeviceConnection(status, dateTime);
        }

        public void WriteTo(At24Cxx device, ushort startAddress)
        {
            var bytes = new byte[PacketLength];
            var seconds = LastTimeSynchronized.ToUnixTimeSeconds();
            using var hash = new Crc8();

            bytes[sizeof(byte)] = (byte)Status;
            Array.Copy(BitConverter.GetBytes(seconds), 0, bytes, sizeof(byte) + sizeof(sbyte), sizeof(long));
            bytes[0] = hash.Compute(bytes, sizeof(byte));

            device.Write(startAddress, bytes);
        }
    }
}

#nullable restore