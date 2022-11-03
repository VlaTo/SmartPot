using System;
using System.Text;

namespace SmartPot.Application.Core
{
    internal sealed class PayloadReader2
    {
        private readonly Memory<byte> original;
        private Memory<byte> current;

        public PayloadReader2(Span<byte> span)
            : this(span.ToArray())
        {
        }

        public PayloadReader2(byte[] bytes)
        {
            original = new Memory<byte>(bytes);
            current = original;
        }

        public byte ReadByte()
        {
            var memory = current.Slice(0, sizeof(byte));
            current = current.Slice(memory.Length);
            return memory.Span[0];
        }

        public string ReadString(Encoding encoding)
        {
            var prefix = current.Slice(0, sizeof(byte)).Span;
            var data = current.Slice(prefix.Length, prefix[0]).Span;
            current = current.Slice(prefix.Length + data.Length);
            return encoding.GetString(data);
        }

        public Span<byte> ReadBytes()
        {
            var prefix = current.Slice(0, sizeof(byte)).Span;
            var data = current.Slice(prefix.Length, prefix[0]).Span;
            current = current.Slice(prefix.Length + data.Length);
            return data;
        }
    }
}