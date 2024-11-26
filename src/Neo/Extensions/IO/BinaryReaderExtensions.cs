// Copyright (C) 2015-2024 The Neo Project.
//
// BinaryReaderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;

namespace Neo.Extensions
{
    public static class BinaryReaderExtensions
    {
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
            var fb = reader.ReadByte();
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
    }
}
