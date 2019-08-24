using Neo.Cryptography;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Neo.IO.Data.LevelDB
{
    public struct Slice : IComparable<Slice>, IEquatable<Slice>
    {
        internal byte[] buffer;

        internal Slice(IntPtr data, UIntPtr length)
        {
            buffer = new byte[(int)length];
            Marshal.Copy(data, buffer, 0, (int)length);
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
            if (ReferenceEquals(null, obj)) return false;
            if (!(obj is Slice)) return false;
            return Equals((Slice)obj);
        }

        public override int GetHashCode()
        {
            return (int)buffer.Murmur32(0);
        }

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

        public static implicit operator Slice(byte[] data)
        {
            return new Slice { buffer = data };
        }

        public static implicit operator Slice(bool data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(byte data)
        {
            return new Slice { buffer = new[] { data } };
        }

        public static implicit operator Slice(double data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(short data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(int data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(long data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(float data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(string data)
        {
            return new Slice { buffer = Encoding.UTF8.GetBytes(data) };
        }

        public static implicit operator Slice(ushort data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(uint data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static implicit operator Slice(ulong data)
        {
            return new Slice { buffer = BitConverter.GetBytes(data) };
        }

        public static bool operator <(Slice x, Slice y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator <=(Slice x, Slice y)
        {
            return x.CompareTo(y) <= 0;
        }

        public static bool operator >(Slice x, Slice y)
        {
            return x.CompareTo(y) > 0;
        }

        public static bool operator >=(Slice x, Slice y)
        {
            return x.CompareTo(y) >= 0;
        }

        public static bool operator ==(Slice x, Slice y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Slice x, Slice y)
        {
            return !x.Equals(y);
        }
    }
}
