using AntShares.IO;
using AntShares.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections;

namespace AntShares.Core.Scripts
{
    internal class StackItem : IEquatable<StackItem>, IReadOnlyCollection<StackItem>
    {
        public readonly bool IsArray;
        private byte[] _bytes;
        private ISerializable _object;
        private StackItem[] _array;
        private byte[] _hash;

        public StackItem this[int index]
        {
            get
            {
                if (IsArray) return _array[index];
                if (index == 0) return this;
                throw new IndexOutOfRangeException();
            }
        }

        public int Count => _array?.Length ?? 1;

        public StackItem(byte[] value)
        {
            this.IsArray = false;
            this._bytes = value;
        }

        public StackItem(byte[][] value)
        {
            this.IsArray = true;
            this._array = value.Select(p => new StackItem(p)).ToArray();
        }

        public StackItem(ISerializable value)
        {
            this.IsArray = false;
            this._object = value;
        }

        public StackItem(ISerializable[] value)
        {
            this.IsArray = true;
            this._array = value.Select(p => new StackItem(p)).ToArray();
        }

        public StackItem(IEnumerable<StackItem> value)
        {
            this.IsArray = true;
            this._array = value.ToArray();
        }

        public bool Equals(StackItem other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (IsArray || other.IsArray) return false;
            if (_bytes == null && _object == null && other._bytes == null && other._object == null)
                return true;
            return ((byte[])this).SequenceEqual((byte[])other);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StackItem)) return false;
            return Equals((StackItem)obj);
        }

        public T[] GetArray<T>() where T : class, ISerializable
        {
            if (_array == null)
            {
                _array = new[] { this };
            }
            return _array.Select(p => p.GetInterface<T>()).ToArray();
        }

        public bool[] GetBooleanArray()
        {
            if (_array == null)
            {
                _array = new[] { this };
            }
            return _array.Select(p => (bool)p).ToArray();
        }

        public byte[][] GetBytesArray()
        {
            if (_array == null)
            {
                _array = new[] { this };
            }
            return _array.Select(p => (byte[])p).ToArray();
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            if (IsArray)
                foreach (StackItem item in _array)
                    yield return item;
            else
                yield return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            if (IsArray) return base.GetHashCode();
            if (_bytes == null && _object == null) return 0;
            if (_hash == null)
                _hash = ((byte[])this).Sha256();
            return BitConverter.ToInt32(_hash, 0);
        }

        public BigInteger[] GetIntArray()
        {
            if (_array == null)
            {
                _array = new[] { this };
            }
            return _array.Select(p => (BigInteger)p).ToArray();
        }

        public T GetInterface<T>() where T : class, ISerializable
        {
            if (IsArray)
            {
                if (_array.Length == 0) return null;
                return _array[0].GetInterface<T>();
            }
            if (_object == null)
            {
                if (_bytes == null) return null;
                if (typeof(T) == typeof(Transaction))
                {
                    _object = Transaction.DeserializeFrom(_bytes);
                }
                else
                {
                    try
                    {
                        _object = Activator.CreateInstance<T>();
                        using (MemoryStream ms = new MemoryStream(_bytes))
                        using (BinaryReader r = new BinaryReader(ms))
                        {
                            _object.Deserialize(r);
                        }
                    }
                    catch (Exception ex) when (ex is FormatException || ex is MissingMethodException) { }
                }
            }
            return _object as T;
        }

        public static implicit operator StackItem(int value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(int[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(uint value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(uint[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(long value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(long[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(ulong value)
        {
            return (BigInteger)value;
        }

        public static implicit operator StackItem(ulong[] value)
        {
            return value.Select(p => (BigInteger)p).ToArray();
        }

        public static implicit operator StackItem(BigInteger value)
        {
            return new StackItem(value.ToByteArray());
        }

        public static implicit operator StackItem(BigInteger[] value)
        {
            return value.Select(p => p.ToByteArray()).ToArray();
        }

        public static implicit operator StackItem(bool value)
        {
            return new StackItem(value ? new[] { (byte)1 } : new byte[0]);
        }

        public static implicit operator StackItem(bool[] value)
        {
            return value.Select(p => p ? new[] { (byte)1 } : new byte[0]).ToArray();
        }

        public static implicit operator StackItem(byte[] value)
        {
            return new StackItem(value);
        }

        public static implicit operator StackItem(byte[][] value)
        {
            return new StackItem(value);
        }

        public static explicit operator BigInteger(StackItem value)
        {
            if (value.IsArray)
            {
                if (value._array.Length == 0) throw new InvalidCastException();
                return (BigInteger)value._array[0];
            }
            if (value._bytes == null)
            {
                if (value._object == null) throw new InvalidCastException();
                value._bytes = value._object.ToArray();
            }
            return new BigInteger(value._bytes);
        }

        public static implicit operator bool(StackItem value)
        {
            if (value == null) return false;
            if (value.IsArray) return value._array.Length > 0;
            if (value._object != null) return true;
            if (value._bytes == null) return false;
            return value._bytes.Any(p => p != 0);
        }

        public static explicit operator byte[] (StackItem value)
        {
            if (value.IsArray)
            {
                if (value._array.Length == 0) throw new InvalidCastException();
                return (byte[])value._array[0];
            }
            if (value._bytes == null)
            {
                if (value._object == null) throw new InvalidCastException();
                value._bytes = value._object.ToArray();
            }
            return value._bytes;
        }
    }
}
