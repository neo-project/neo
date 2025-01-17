// Copyright (C) 2015-2025 The Neo Project.
//
// ByteExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;

namespace Neo.Extensions
{
    public static class ByteExtensions
    {
        /// <summary>
        /// Compresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The data to be compressed.</param>
        /// <returns>The compressed data.</returns>
        public static ReadOnlyMemory<byte> CompressLz4(this byte[] data)
        {
            return data.AsSpan().CompressLz4();
        }

        /// <summary>
        /// Decompresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The compressed data.</param>
        /// <param name="maxOutput">The maximum data size after decompression.</param>
        /// <returns>The original data.</returns>
        public static byte[] DecompressLz4(this byte[] data, int maxOutput)
        {
            return data.AsSpan().DecompressLz4(maxOutput);
        }

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
    }
}
