using AntShares.Cryptography;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AntShares.Implementations.Blockchains.LevelDB
{
    internal struct Slice : IComparable<Slice>, IEquatable<Slice>
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
            return BitConverter.ToInt32(buffer.Sha256(), 0);
        }

        public byte[] ToArray()
        {
            return buffer ?? new byte[0];
        }

        public bool ToBoolean()
        {
            if (buffer.Length != sizeof(bool))
                throw new InvalidCastException();
            return BitConverter.ToBoolean(buffer, 0);
        }

        public byte ToByte()
        {
            if (buffer.Length != sizeof(byte))
                throw new InvalidCastException();
            return buffer[0];
        }

        public double ToDouble()
        {
            if (buffer.Length != sizeof(double))
                throw new InvalidCastException();
            return BitConverter.ToDouble(buffer, 0);
        }

        public short ToInt16()
        {
            if (buffer.Length != sizeof(short))
                throw new InvalidCastException();
            return BitConverter.ToInt16(buffer, 0);
        }

        public int ToInt32()
        {
            if (buffer.Length != sizeof(int))
                throw new InvalidCastException();
            return BitConverter.ToInt32(buffer, 0);
        }

        public long ToInt64()
        {
            if (buffer.Length != sizeof(long))
                throw new InvalidCastException();
            return BitConverter.ToInt64(buffer, 0);
        }

        public float ToSingle()
        {
            if (buffer.Length != sizeof(float))
                throw new InvalidCastException();
            return BitConverter.ToSingle(buffer, 0);
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(buffer);
        }

        public ushort ToUInt16()
        {
            if (buffer.Length != sizeof(ushort))
                throw new InvalidCastException();
            return BitConverter.ToUInt16(buffer, 0);
        }

        public uint ToUInt32()
        {
            if (buffer.Length != sizeof(uint))
                throw new InvalidCastException();
            return BitConverter.ToUInt32(buffer, 0);
        }

        public ulong ToUInt64()
        {
            if (buffer.Length != sizeof(ulong))
                throw new InvalidCastException();
            return BitConverter.ToUInt64(buffer, 0);
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
