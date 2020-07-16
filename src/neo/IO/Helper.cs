using K4os.Compression.LZ4;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Neo.IO
{
    public static class Helper
    {
        public static T AsSerializable<T>(this byte[] value, int start = 0) where T : ISerializable, new()
        {
            using (MemoryStream ms = new MemoryStream(value, start, value.Length - start, false))
            using (BinaryReader reader = new BinaryReader(ms, Utility.StrictUTF8))
            {
                return reader.ReadSerializable<T>();
            }
        }

        public static unsafe T AsSerializable<T>(this ReadOnlySpan<byte> value) where T : ISerializable, new()
        {
            if (value.IsEmpty) throw new FormatException();
            fixed (byte* pointer = value)
            {
                using UnmanagedMemoryStream ms = new UnmanagedMemoryStream(pointer, value.Length);
                using BinaryReader reader = new BinaryReader(ms, Utility.StrictUTF8);
                return reader.ReadSerializable<T>();
            }
        }

        public static ISerializable AsSerializable(this byte[] value, Type type)
        {
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidCastException();
            ISerializable serializable = (ISerializable)Activator.CreateInstance(type);
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms, Utility.StrictUTF8))
            {
                serializable.Deserialize(reader);
            }
            return serializable;
        }

        public static T[] AsSerializableArray<T>(this byte[] value, int max = 0x1000000) where T : ISerializable, new()
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms, Utility.StrictUTF8))
            {
                return reader.ReadSerializableArray<T>(max);
            }
        }

        public static unsafe T[] AsSerializableArray<T>(this ReadOnlySpan<byte> value, int max = 0x1000000) where T : ISerializable, new()
        {
            if (value.IsEmpty) throw new FormatException();
            fixed (byte* pointer = value)
            {
                using UnmanagedMemoryStream ms = new UnmanagedMemoryStream(pointer, value.Length);
                using BinaryReader reader = new BinaryReader(ms, Utility.StrictUTF8);
                return reader.ReadSerializableArray<T>(max);
            }
        }

        public static byte[] CompressLz4(this byte[] data)
        {
            int maxLength = LZ4Codec.MaximumOutputSize(data.Length);
            using var buffer = MemoryPool<byte>.Shared.Rent(maxLength);
            int length = LZ4Codec.Encode(data, buffer.Memory.Span);
            byte[] result = new byte[sizeof(uint) + length];
            BinaryPrimitives.WriteInt32LittleEndian(result, data.Length);
            buffer.Memory[..length].CopyTo(result.AsMemory(4));
            return result;
        }

        public static byte[] DecompressLz4(this byte[] data, int maxOutput)
        {
            int length = BinaryPrimitives.ReadInt32LittleEndian(data);
            if (length < 0 || length > maxOutput) throw new FormatException();
            byte[] result = new byte[length];
            if (LZ4Codec.Decode(data.AsSpan(4), result) != length)
                throw new FormatException();
            return result;
        }

        public static void FillBuffer(this BinaryReader reader, Span<byte> buffer)
        {
            while (!buffer.IsEmpty)
            {
                int count = reader.Read(buffer);
                if (count == 0) throw new EndOfStreamException();
                buffer = buffer[count..];
            }
        }

        public static int GetVarSize(int value)
        {
            if (value < 0xFD)
                return sizeof(byte);
            else if (value <= 0xFFFF)
                return sizeof(byte) + sizeof(ushort);
            else
                return sizeof(byte) + sizeof(uint);
        }

        public static int GetVarSize<T>(this IReadOnlyCollection<T> value)
        {
            int value_size;
            Type t = typeof(T);
            if (typeof(ISerializable).IsAssignableFrom(t))
            {
                value_size = value.OfType<ISerializable>().Sum(p => p.Size);
            }
            else if (t.GetTypeInfo().IsEnum)
            {
                int element_size;
                Type u = t.GetTypeInfo().GetEnumUnderlyingType();
                if (u == typeof(sbyte) || u == typeof(byte))
                    element_size = 1;
                else if (u == typeof(short) || u == typeof(ushort))
                    element_size = 2;
                else if (u == typeof(int) || u == typeof(uint))
                    element_size = 4;
                else //if (u == typeof(long) || u == typeof(ulong))
                    element_size = 8;
                value_size = value.Count * element_size;
            }
            else
            {
                value_size = value.Count * Marshal.SizeOf<T>();
            }
            return GetVarSize(value.Count) + value_size;
        }

        public static int GetVarSize(this string value)
        {
            int size = Utility.StrictUTF8.GetByteCount(value);
            return GetVarSize(size) + size;
        }

        public static byte[] ReadFixedBytes(this BinaryReader reader, int size)
        {
            var index = 0;
            var data = new byte[size];

            while (size > 0)
            {
                var bytesRead = reader.Read(data, index, size);

                if (bytesRead <= 0)
                {
                    throw new FormatException();
                }

                size -= bytesRead;
                index += bytesRead;
            }

            return data;
        }

        public static string ReadFixedString(this BinaryReader reader, int length)
        {
            byte[] data = reader.ReadFixedBytes(length);
            return Utility.StrictUTF8.GetString(data.TakeWhile(p => p != 0).ToArray());
        }

        public static T[] ReadNullableArray<T>(this BinaryReader reader, int max = 0x1000000) where T : class, ISerializable, new()
        {
            T[] array = new T[reader.ReadVarInt((ulong)max)];
            for (int i = 0; i < array.Length; i++)
                array[i] = reader.ReadBoolean() ? reader.ReadSerializable<T>() : null;
            return array;
        }

        public static T ReadSerializable<T>(this BinaryReader reader) where T : ISerializable, new()
        {
            T obj = new T();
            obj.Deserialize(reader);
            return obj;
        }

        public static T[] ReadSerializableArray<T>(this BinaryReader reader, int max = 0x1000000) where T : ISerializable, new()
        {
            T[] array = new T[reader.ReadVarInt((ulong)max)];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new T();
                array[i].Deserialize(reader);
            }
            return array;
        }

        public static byte[] ReadVarBytes(this BinaryReader reader, int max = 0x1000000)
        {
            return reader.ReadFixedBytes((int)reader.ReadVarInt((ulong)max));
        }

        public static ulong ReadVarInt(this BinaryReader reader, ulong max = ulong.MaxValue)
        {
            byte fb = reader.ReadByte();
            ulong value;
            if (fb == 0xFD)
                value = reader.ReadUInt16();
            else if (fb == 0xFE)
                value = reader.ReadUInt32();
            else if (fb == 0xFF)
                value = reader.ReadUInt64();
            else
                value = fb;
            if (value > max) throw new FormatException();
            return value;
        }

        public static string ReadVarString(this BinaryReader reader, int max = 0x1000000)
        {
            return Utility.StrictUTF8.GetString(reader.ReadVarBytes(max));
        }

        public static byte[] ToArray(this ISerializable value)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Utility.StrictUTF8))
            {
                value.Serialize(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static byte[] ToByteArray<T>(this IReadOnlyCollection<T> value) where T : ISerializable
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Utility.StrictUTF8))
            {
                writer.Write(value);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static void Write(this BinaryWriter writer, ISerializable value)
        {
            value.Serialize(writer);
        }

        public static void Write<T>(this BinaryWriter writer, IReadOnlyCollection<T> value) where T : ISerializable
        {
            writer.WriteVarInt(value.Count);
            foreach (T item in value)
            {
                item.Serialize(writer);
            }
        }

        public static void WriteFixedString(this BinaryWriter writer, string value, int length)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > length)
                throw new ArgumentException();
            byte[] bytes = Utility.StrictUTF8.GetBytes(value);
            if (bytes.Length > length)
                throw new ArgumentException();
            writer.Write(bytes);
            if (bytes.Length < length)
                writer.Write(stackalloc byte[length - bytes.Length]);
        }

        public static void WriteNullableArray<T>(this BinaryWriter writer, T[] value) where T : class, ISerializable
        {
            writer.WriteVarInt(value.Length);
            foreach (var item in value)
            {
                bool isNull = item is null;
                writer.Write(!isNull);
                if (isNull) continue;
                item.Serialize(writer);
            }
        }

        public static void WriteVarBytes(this BinaryWriter writer, ReadOnlySpan<byte> value)
        {
            writer.WriteVarInt(value.Length);
            writer.Write(value);
        }

        public static void WriteVarInt(this BinaryWriter writer, long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException();
            if (value < 0xFD)
            {
                writer.Write((byte)value);
            }
            else if (value <= 0xFFFF)
            {
                writer.Write((byte)0xFD);
                writer.Write((ushort)value);
            }
            else if (value <= 0xFFFFFFFF)
            {
                writer.Write((byte)0xFE);
                writer.Write((uint)value);
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(value);
            }
        }

        public static void WriteVarString(this BinaryWriter writer, string value)
        {
            writer.WriteVarBytes(Utility.StrictUTF8.GetBytes(value));
        }
    }
}
