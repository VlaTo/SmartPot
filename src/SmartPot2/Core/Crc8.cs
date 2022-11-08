using System;

#nullable enable

namespace SmartPot2.Core
{
    internal sealed class Crc8 : IDisposable
    {
        private byte[]? table;
        private const byte polynomial = 0xD5;

        public Crc8()
        {
            table = new byte[256];

            for (var index = 0; index < 256; ++index)
            {
                var temp = index;

                for (var position = 0; position < 8; position++)
                {
                    if (0 != (temp & 0x80))
                    {
                        temp = (temp << 1) ^ polynomial;
                    }
                    else
                    {
                        temp <<= 1;
                    }
                }

                table[index] = (byte)temp;
            }
        }

        public byte Compute(byte[] bytes, int startIndex = 0)
        {
            byte crc = 0;

            for (var index = startIndex; index < bytes.Length; index++)
            {
                unchecked
                {
                    crc = (byte)(table![(crc ^ bytes[index]) & 0xFF] ^ (crc >> 8));
                }
            }

            return crc;
        }

        public void Dispose()
        {
            table = null;
        }
    }
}

#nullable restore