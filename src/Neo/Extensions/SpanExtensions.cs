// Copyright (C) 2015-2024 The Neo Project.
//
// SpanExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using K4os.Compression.LZ4;
using System;
using System.Buffers.Binary;

namespace Neo.Extensions
{
    public static class SpanExtensions
    {
        /// <summary>
        /// Compresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The data to be compressed.</param>
        /// <returns>The compressed data.</returns>
        public static ReadOnlyMemory<byte> CompressLz4(this byte[] data)
        {
            return CompressLz4(data.AsSpan());
        }

        /// <summary>
        /// Compresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The data to be compressed.</param>
        /// <returns>The compressed data.</returns>
        public static ReadOnlyMemory<byte> CompressLz4(this ReadOnlySpan<byte> data)
        {
            var maxLength = LZ4Codec.MaximumOutputSize(data.Length);
            var buffer = new byte[sizeof(uint) + maxLength];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, data.Length);
            var length = LZ4Codec.Encode(data, buffer.AsSpan(sizeof(uint)));
            return buffer.AsMemory(0, sizeof(uint) + length);
        }

        /// <summary>
        /// Decompresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The compressed data.</param>
        /// <param name="maxOutput">The maximum data size after decompression.</param>
        /// <returns>The original data.</returns>
        public static byte[] DecompressLz4(this byte[] data, int maxOutput)
        {
            return DecompressLz4(data.AsSpan(), maxOutput);
        }

        /// <summary>
        /// Decompresses the specified data using the LZ4 algorithm.
        /// </summary>
        /// <param name="data">The compressed data.</param>
        /// <param name="maxOutput">The maximum data size after decompression.</param>
        /// <returns>The original data.</returns>
        public static byte[] DecompressLz4(this ReadOnlySpan<byte> data, int maxOutput)
        {
            var length = BinaryPrimitives.ReadInt32LittleEndian(data);
            if (length < 0 || length > maxOutput) throw new FormatException();
            var result = new byte[length];
            if (LZ4Codec.Decode(data[4..], result) != length)
                throw new FormatException();
            return result;
        }
    }
}
