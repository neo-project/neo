using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.IO.Data
{
    public class SliceBuilder
    {
        private readonly List<byte> _data = new List<byte>();

        private SliceBuilder() { }

        public SliceBuilder Add(byte value)
        {
            _data.Add(value);
            return this;
        }

        public SliceBuilder Add(ushort value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(uint value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(long value)
        {
            _data.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(IEnumerable<byte> value)
        {
            _data.AddRange(value);
            return this;
        }

        public SliceBuilder Add(string value)
        {
            _data.AddRange(Encoding.UTF8.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(ISerializable value)
        {
            _data.AddRange(value.ToArray());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SliceBuilder Begin()
        {
            return new SliceBuilder();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SliceBuilder Begin(byte prefix)
        {
            return new SliceBuilder().Add(prefix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(SliceBuilder value)
        {
            return value._data.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte[](SliceBuilder value)
        {
            return value._data.ToArray();
        }
    }
}
