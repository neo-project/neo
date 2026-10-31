// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SpanExtensions_Lz4.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using System;

namespace Neo.UnitTests.Extensions
{
    /// <summary>
    /// Span/ReadOnlySpan LZ4 overloads and empty-input edges beyond UT_IOHelper.
    /// </summary>
    [TestClass]
    public class UT_SpanExtensions_Lz4
    {
        [TestMethod]
        public void CompressDecompress_ReadOnlySpan_Empty()
        {
            ReadOnlySpan<byte> empty = ReadOnlySpan<byte>.Empty;
            var compressed = empty.CompressLz4();
            Assert.IsTrue(compressed.Length >= sizeof(int));
            var restored = compressed.Span.DecompressLz4(maxOutput: 0);
            Assert.HasCount(0, restored);
        }

        [TestMethod]
        public void CompressDecompress_Span_RoundTrip()
        {
            byte[] data = [1, 2, 3, 4, 5, 1, 2, 3, 4, 5];
            var compressed = data.AsSpan().CompressLz4();
            var restored = compressed.Span.DecompressLz4(maxOutput: 64);
            Assert.IsTrue(data.AsSpan().SequenceEqual(restored));
        }

        [TestMethod]
        public void Decompress_Span_MismatchedLength_Throws()
        {
            byte[] data = [9, 8, 7];
            var compressed = data.AsSpan().CompressLz4().ToArray();
            // Corrupt stored length header to exceed maxOutput
            compressed[0] = 0xFF;
            compressed[1] = 0xFF;
            compressed[2] = 0x00;
            compressed[3] = 0x00;
            Assert.ThrowsExactly<FormatException>(() => compressed.AsSpan().DecompressLz4(maxOutput: 10));
        }
    }
}
