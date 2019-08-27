using Neo.Cryptography;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Neo.IO.Data
{
    public struct Slice : IComparable<Slice>, IEquatable<Slice>
    {
        internal readonly byte[] buffer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="length">Length</param>
        internal Slice(IntPtr data, UIntPtr length)
        {
            buffer = new byte[(int)length];
            Marshal.Copy(data, buffer, 0, (int)length);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="input">Input</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Slice(byte[] input)
        {
            buffer = input;
        }

        public int CompareTo(Slice other)
        {
            for (int i = 0; i < buffer.Length && i < other.buffer.Length; i++)
            {
                int r = buffer[i].CompareTo(other.buffer[i]);
                if (r != 0) return r;
            }
            return buffer.Length.CompareTo(other.buffer.Length);
        }

        public bool Equals(Slice other)
        {
            if (buffer.Length != other.buffer.Length) return false;
            return buffer.SequenceEqual(other.buffer);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Slice slide)) return false;
            return Equals(slide);
        }

        public override int GetHashCode()
        {
            return (int)buffer.Murmur32(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ToArray()
        {
            return buffer ?? new byte[0];
        }

        unsafe public bool ToBoolean()
        {
            if (buffer.Length != sizeof(bool))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((bool*)pbyte);
            }
        }

        public byte ToByte()
        {
            if (buffer.Length != sizeof(byte))
                throw new InvalidCastException();
            return buffer[0];
        }

        unsafe public double ToDouble()
        {
            if (buffer.Length != sizeof(double))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((double*)pbyte);
            }
        }

        unsafe public short ToInt16()
        {
            if (buffer.Length != sizeof(short))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((short*)pbyte);
            }
        }

        unsafe public int ToInt32()
        {
            if (buffer.Length != sizeof(int))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((int*)pbyte);
            }
        }

        unsafe public long ToInt64()
        {
            if (buffer.Length != sizeof(long))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((long*)pbyte);
            }
        }

        unsafe public float ToSingle()
        {
            if (buffer.Length != sizeof(float))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((float*)pbyte);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return Encoding.UTF8.GetString(buffer);
        }

        unsafe public ushort ToUInt16()
        {
            if (buffer.Length != sizeof(ushort))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((ushort*)pbyte);
            }
        }

        unsafe public uint ToUInt32(int index = 0)
        {
            if (buffer.Length != sizeof(uint) + index)
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[index])
            {
                return *((uint*)pbyte);
            }
        }

        unsafe public ulong ToUInt64()
        {
            if (buffer.Length != sizeof(ulong))
                throw new InvalidCastException();
            fixed (byte* pbyte = &buffer[0])
            {
                return *((ulong*)pbyte);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte[](Slice value)
        {
            return value.buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(byte[] data)
        {
            return new Slice(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(bool data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(byte data)
        {
            return new Slice(new[] { data });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(double data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(short data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(int data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(long data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(float data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(string data)
        {
            return new Slice(Encoding.UTF8.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(ushort data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(uint data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Slice(ulong data)
        {
            return new Slice(BitConverter.GetBytes(data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Slice x, Slice y)
        {
            return x.CompareTo(y) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Slice x, Slice y)
        {
            return x.CompareTo(y) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Slice x, Slice y)
        {
            return x.CompareTo(y) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Slice x, Slice y)
        {
            return x.CompareTo(y) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Slice x, Slice y)
        {
            return x.Equals(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Slice x, Slice y)
        {
            return !x.Equals(y);
        }
    }
}
