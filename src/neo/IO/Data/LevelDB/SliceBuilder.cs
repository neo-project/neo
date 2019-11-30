using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.IO.Data.LevelDB
{
    public class SliceBuilder
    {
        private readonly List<byte> data;

        private SliceBuilder()
        {
            data = new List<byte>();
        }

        private SliceBuilder(params byte[] input)
        {
            data = new List<byte>(input);
        }

        public SliceBuilder Add(byte value)
        {
            data.Add(value);
            return this;
        }

        public SliceBuilder Add(ushort value)
        {
            data.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(uint value)
        {
            data.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(long value)
        {
            data.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(IEnumerable<byte> value)
        {
            if (value != null)
                data.AddRange(value);
            return this;
        }

        public SliceBuilder Add(string value)
        {
            if (value != null)
                data.AddRange(Encoding.UTF8.GetBytes(value));
            return this;
        }

        public SliceBuilder Add(ISerializable value)
        {
            if (value != null)
                data.AddRange(value.ToArray());
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
            return new SliceBuilder(prefix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(SliceBuilder value)
        {
            return value.data.ToArray();
        }
    }
}
