using System;
using System.Device.I2c;

namespace SmartPot2.Devices
{
    internal sealed class At24Cxx : IDisposable
    {
        public enum Size
        {
            Rom32,
            Rom64
        }

        public const byte DefaultI2cAddress = 0x57;

        private readonly I2cDevice device;
        private readonly Size size;

        public At24Cxx(I2cDevice device, Size size)
        {
            this.device = device;
            this.size = size;
        }

        /// <summary>Write at a specific address.</summary>
        /// <param name="address">The address to write.</param>
        /// <param name="data">The byte buffer to write.</param>
        public void Write(ushort address, byte[] data)
        {
            EnsureDataLength(data.Length);

            var buffer = new byte[2 + data.Length];

            buffer[0] = (byte)(address >> 8 & byte.MaxValue);
            buffer[1] = (byte)(address & (uint)byte.MaxValue);

            data.CopyTo(buffer, 2);
            
            device.Write((SpanByte)buffer);
        }

        /// <summary>Read a specific address.</summary>
        /// <param name="address">The address to read.</param>
        /// <param name="numOfBytes">The number of bytes to read.</param>
        /// <returns>The read elements.</returns>
        public byte[] Read(ushort address, int numOfBytes)
        {
            EnsureDataLength(numOfBytes);

            var readBuffer = new byte[numOfBytes];

            device.WriteRead((SpanByte)new byte[2]
                {
                    (byte)(address >> 8 & byte.MaxValue),
                    (byte)(address & (uint)byte.MaxValue)
                },
                (SpanByte)readBuffer
            );

            return readBuffer;
        }

        public int GetMaxLength()
        {
            switch (size)
            {
                case Size.Rom32: return 4096;
                case Size.Rom64: return 8192;
                default: return -1;
            }
        }

        public void Dispose()
        {
            device.Dispose();
        }

        private void EnsureDataLength(int numOfBytes)
        {
            var length = GetMaxLength();

            if (length < numOfBytes)
            {
                throw new InvalidOperationException();
            }
        }
    }
}