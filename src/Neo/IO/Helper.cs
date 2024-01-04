// Copyright (C) 2015-2024 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using K4os.Compression.LZ4;
using Neo.IO.Caching;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Neo.IO
{
    /// <summary>
    /// A helper class for serialization of NEO objects.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> object.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="value">The byte array to be converted.</param>
        /// <param name="start">The offset into the byte array from which to begin using data.</param>
        /// <returns>The converted <see cref="ISerializable"/> object.</returns>
        public static T AsSerializable<T>(this byte[] value, int start = 0) where T : ISerializable, new()
        {
            MemoryReader reader = new(value.AsMemory(start));
            return reader.ReadSerializable<T>();
        }

        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> object.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="value">The byte array to be converted.</param>
        /// <returns>The converted <see cref="ISerializable"/> object.</returns>
        public static T AsSerializable<T>(this ReadOnlyMemory<byte> value) where T : ISerializable, new()
        {
            if (value.IsEmpty) throw new FormatException();
            MemoryReader reader = new(value);
            return reader.ReadSerializable<T>();
        }

        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> object.
        /// </summary>
        /// <param name="value">The byte array to be converted.</param>
        /// <param name="type">The type to convert to.</param>
        /// <returns>The converted <see cref="ISerializable"/> object.</returns>
        public static ISerializable AsSerializable(this ReadOnlyMemory<byte> value, Type type)
        {
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidCastException();
            ISerializable serializable = (ISerializable)Activator.CreateInstance(type);
            MemoryReader reader = new(value);
            serializable.Deserialize(ref reader);
            return serializable;
        }

        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> array.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="value">The byte array to be converted.</param>
        /// <param name="max">The maximum number of elements contained in the converted array.</param>
        /// <returns>The converted <see cref="ISerializable"/> array.</returns>
        public static T[] AsSerializableArray<T>(this byte[] value, int max = 0x1000000) where T : ISerializable, new()
        {
            MemoryReader reader = new(value);
            return reader.ReadSerializableArray<T>(max);
        }

        /// <summary>
        /// Converts a byte array to an <see cref="ISerializable"/> array.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="value">The byte array to be converted.</param>
        /// <param name="max">The maximum number of elements contained in the converted array.</param>
        /// <returns>The converted <see cref="ISerializable"/> array.</returns>
        public static T[] AsSerializableArray<T>(this ReadOnlyMemory<byte> value, int max = 0x1000000) where T : ISerializable, new()
        {
            if (value.IsEmpty) throw new FormatException();
            MemoryReader reader = new(value);
            return reader.ReadSerializableArray<T>(max);
        }

        /// <summary>
        /// Compresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The data to be compressed.</param>
        /// <returns>The compressed data.</returns>
        public static ReadOnlyMemory<byte> CompressLz4(this ReadOnlySpan<byte> data)
        {
            int maxLength = LZ4Codec.MaximumOutputSize(data.Length);
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(uint) + maxLength);
            BinaryPrimitives.WriteInt32LittleEndian(buffer, data.Length);
            int length = LZ4Codec.Encode(data, buffer.AsSpan(sizeof(uint)));
            return buffer.AsMemory(0, sizeof(uint) + length);
        }

        /// <summary>
        /// Decompresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The compressed data.</param>
        /// <param name="maxOutput">The maximum data size after decompression.</param>
        /// <returns>The original data.</returns>
        public static byte[] DecompressLz4(this ReadOnlySpan<byte> data, int maxOutput)
        {
            int length = BinaryPrimitives.ReadInt32LittleEndian(data);
            if (length < 0 || length > maxOutput) throw new FormatException();
            byte[] result = GC.AllocateUninitializedArray<byte>(length);
            if (LZ4Codec.Decode(data[4..], result) != length)
                throw new FormatException();
            return result;
        }

        /// <summary>
        /// Gets the size of variable-length of the data.
        /// </summary>
        /// <param name="value">The length of the data.</param>
        /// <returns>The size of variable-length of the data.</returns>
        public static int GetVarSize(int value)
        {
            if (value < 0xFD)
                return sizeof(byte);
            else if (value <= 0xFFFF)
                return sizeof(byte) + sizeof(ushort);
            else
                return sizeof(byte) + sizeof(uint);
        }

        /// <summary>
        /// Gets the size of the specified array encoded in variable-length encoding.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="value">The specified array.</param>
        /// <returns>The size of the array.</returns>
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

        /// <summary>
        /// Gets the size of the specified array encoded in variable-length encoding.
        /// </summary>
        /// <param name="value">The specified array.</param>
        /// <returns>The size of the array.</returns>
        public static int GetVarSize(this ReadOnlyMemory<byte> value)
        {
            return GetVarSize(value.Length) + value.Length;
        }

        /// <summary>
        /// Gets the size of the specified <see cref="string"/> encoded in variable-length encoding.
        /// </summary>
        /// <param name="value">The specified <see cref="string"/>.</param>
        /// <returns>The size of the <see cref="string"/>.</returns>
        public static int GetVarSize(this string value)
        {
            int size = Utility.StrictUTF8.GetByteCount(value);
            return GetVarSize(size) + size;
        }

        /// <summary>
        /// Reads a byte array of the specified size from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <param name="size">The size of the byte array.</param>
        /// <returns>The byte array read from the <see cref="BinaryReader"/>.</returns>
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

        /// <summary>
        /// Reads an <see cref="ISerializable"/> array from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="max">The maximum number of elements in the array.</param>
        /// <returns>The array read from the <see cref="MemoryReader"/>.</returns>
        public static T[] ReadNullableArray<T>(this ref MemoryReader reader, int max = 0x1000000) where T : class, ISerializable, new()
        {
            T[] array = new T[reader.ReadVarInt((ulong)max)];
            for (int i = 0; i < array.Length; i++)
                array[i] = reader.ReadBoolean() ? reader.ReadSerializable<T>() : null;
            return array;
        }

        /// <summary>
        /// Reads an <see cref="ISerializable"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ISerializable"/> object.</typeparam>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <returns>The object read from the <see cref="MemoryReader"/>.</returns>
        public static T ReadSerializable<T>(this ref MemoryReader reader) where T : ISerializable, new()
        {
            T obj = new();
            obj.Deserialize(ref reader);
            return obj;
        }

        /// <summary>
        /// Reads an <see cref="ISerializable"/> array from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="max">The maximum number of elements in the array.</param>
        /// <returns>The array read from the <see cref="MemoryReader"/>.</returns>
        public static T[] ReadSerializableArray<T>(this ref MemoryReader reader, int max = 0x1000000) where T : ISerializable, new()
        {
            T[] array = new T[reader.ReadVarInt((ulong)max)];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new T();
                array[i].Deserialize(ref reader);
            }
            return array;
        }

        /// <summary>
        /// Reads a byte array from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <param name="max">The maximum size of the byte array.</param>
        /// <returns>The byte array read from the <see cref="BinaryReader"/>.</returns>
        public static byte[] ReadVarBytes(this BinaryReader reader, int max = 0x1000000)
        {
            return reader.ReadFixedBytes((int)reader.ReadVarInt((ulong)max));
        }

        /// <summary>
        /// Reads an integer from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <param name="max">The maximum value of the integer.</param>
        /// <returns>The integer read from the <see cref="BinaryReader"/>.</returns>
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

        /// <summary>
        /// Converts an <see cref="ISerializable"/> object to a byte array.
        /// </summary>
        /// <param name="value">The <see cref="ISerializable"/> object to be converted.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] ToArray(this ISerializable value)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
            value.Serialize(writer);
            writer.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Converts an <see cref="ISerializable"/> array to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="value">The <see cref="ISerializable"/> array to be converted.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] ToByteArray<T>(this IReadOnlyCollection<T> value) where T : ISerializable
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms, Utility.StrictUTF8, true);
            writer.Write(value);
            writer.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Writes an <see cref="ISerializable"/> object into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The <see cref="ISerializable"/> object to be written.</param>
        public static void Write(this BinaryWriter writer, ISerializable value)
        {
            value.Serialize(writer);
        }

        /// <summary>
        /// Writes an <see cref="ISerializable"/> array into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The <see cref="ISerializable"/> array to be written.</param>
        public static void Write<T>(this BinaryWriter writer, IReadOnlyCollection<T> value) where T : ISerializable
        {
            writer.WriteVarInt(value.Count);
            foreach (T item in value)
            {
                item.Serialize(writer);
            }
        }

        /// <summary>
        /// Writes a <see cref="string"/> into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The <see cref="string"/> to be written.</param>
        /// <param name="length">The fixed size of the <see cref="string"/>.</param>
        public static void WriteFixedString(this BinaryWriter writer, string value, int length)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > length)
                throw new ArgumentException(null, nameof(value));
            byte[] bytes = Utility.StrictUTF8.GetBytes(value);
            if (bytes.Length > length)
                throw new ArgumentException(null, nameof(value));
            writer.Write(bytes);
            if (bytes.Length < length)
                writer.Write(stackalloc byte[length - bytes.Length]);
        }

        /// <summary>
        /// Writes an <see cref="ISerializable"/> array into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The <see cref="ISerializable"/> array to be written.</param>
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

        /// <summary>
        /// Writes a byte array into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The byte array to be written.</param>
        public static void WriteVarBytes(this BinaryWriter writer, ReadOnlySpan<byte> value)
        {
            writer.WriteVarInt(value.Length);
            writer.Write(value);
        }

        /// <summary>
        /// Writes an integer into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The integer to be written.</param>
        public static void WriteVarInt(this BinaryWriter writer, long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
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

        /// <summary>
        /// Writes a <see cref="string"/> into a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        /// <param name="value">The <see cref="string"/> to be written.</param>
        public static void WriteVarString(this BinaryWriter writer, string value)
        {
            writer.WriteVarBytes(Utility.StrictUTF8.GetBytes(value));
        }

        internal static void Remove<T>(this HashSet<T> set, HashSetCache<T> other)
            where T : IEquatable<T>
        {
            if (set.Count > other.Count)
            {
                set.ExceptWith(other);
            }
            else
            {
                set.RemoveWhere(u => other.Contains(u));
            }
        }
    }
}
