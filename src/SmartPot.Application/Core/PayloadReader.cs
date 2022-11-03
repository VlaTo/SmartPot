
#nullable enable

using System;
using System.Text;

namespace SmartPot.Application.Core
{
    internal sealed class PayloadReader
    {
        private readonly byte[] bytes;
        private int position;

        public PayloadReader(byte[] bytes)
        {
            this.bytes = bytes;
            position = -1;
        }

        public byte ReadByte()
        {
            if (0 > position)
            {
                position = 0;
            }

            var availableLength = bytes.Length - position;

            if (1 > availableLength)
            {
                throw new Exception();
            }

            return bytes[position++];
        }

        public string ReadString(Encoding encoding)
        {
            var length = ReadByte();
            var buffer = ReadBytesInternal(length);
            return encoding.GetString(buffer);
        }

        public byte[] ReadBytes()
        {
            var length = ReadByte();
            return ReadBytesInternal(length);
        }

        private byte[] ReadBytesInternal(byte count)
        {
            if (0 > position)
            {
                position = 0;
            }

            var availableLength = bytes.Length - position;

            if (count > availableLength)
            {
                throw new Exception();
            }

            var buffer = new byte[count];
            
            Array.Copy(bytes, position, buffer, 0, count);
            position += count;

            return buffer;
        }
    }
}

#nullable restore
