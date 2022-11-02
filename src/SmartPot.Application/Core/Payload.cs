
#nullable enable

using System;
using System.IO;
using System.Text;

namespace SmartPot.Application.Core
{
    internal sealed class Payload
    {
        private readonly MemoryStream stream;

        public Payload()
        {
            stream = new MemoryStream();
        }

        public Payload Write(byte value)
        {
            stream.WriteByte(value);
            return this;
        }

        public Payload Write(string? value, Encoding encoding)
        {
            if (String.IsNullOrEmpty(value))
            {
                stream.WriteByte(0);
            }
            else
            {
                var bytes = encoding.GetBytes(value);

                stream.WriteByte((byte)bytes.Length);
                stream.Write(bytes);
            }

            return this;
        }
        
        public Payload Write(byte[]? bytes)
        {
            if (null == bytes || 0 == bytes.Length)
            {
                stream.WriteByte(0);
            }
            else
            {
                stream.WriteByte((byte)bytes.Length);
                stream.Write(bytes);
            }

            return this;
        }
        
        public Payload Write(Span<byte> span)
        {
            stream.WriteByte((byte)span.Length);
            stream.Write(span);
            return this;
        }

        public byte[] Build(bool crc)
        {
            stream.Flush();

            var bytes = stream.ToArray();

            if (crc)
            {
                var payload = new byte[bytes.Length + 1];

                byte acc = 0;

                for (var index = 0; index < bytes.Length; index++)
                {
                    unchecked
                    {
                        acc += bytes[index];
                    }

                    payload[index] = bytes[index];
                }

                payload[^1] = acc;
                bytes = payload;
            }

            return bytes;
        }
    }
}

#nullable restore