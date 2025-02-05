// Copyright (C) 2015-2025 The Neo Project.
//
// BinaryWriterExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.Extensions
{
    public static class BinaryWriterExtensions
    {
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
        public static void Write<T>(this BinaryWriter writer, IReadOnlyCollection<T> value)
            where T : ISerializable
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
            var bytes = Utility.StrictUTF8.GetBytes(value);
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
        public static void WriteNullableArray<T>(this BinaryWriter writer, T[] value)
            where T : class, ISerializable
        {
            writer.WriteVarInt(value.Length);
            foreach (var item in value)
            {
                var isNull = item is null;
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
    }
}
