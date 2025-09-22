// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkDataFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.VM.Benchmark.Infrastructure
{
    internal static class BenchmarkDataFactory
    {
        private static readonly ExecutionEngineLimits Limits = ExecutionEngineLimits.Default;
        private static int ClampLength(int length)
        {
            return Math.Clamp(length, 1, (int)Limits.MaxItemSize);
        }

        public static byte[] CreateByteArray(int length, byte fill = 0x42)
        {
            length = ClampLength(length);
            var buffer = new byte[length];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = unchecked((byte)(fill + i));
            return buffer;
        }

        public static IReadOnlyList<byte[]> CreateByteSegments(int count, int segmentLength, byte seed = 0x10)
        {
            count = Math.Max(1, count);
            segmentLength = ClampLength(segmentLength);
            var result = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                result[i] = CreateByteArray(segmentLength, (byte)(seed + i));
            }
            return result;
        }

        public static byte[] CreateIteratorKey(int index, byte prefix = 0xC1)
        {
            return new[] { prefix, unchecked((byte)index) };
        }

        public static string CreateString(int length, char seed = 'a')
        {
            length = ClampLength(length);
            var buffer = new char[length];
            for (int i = 0; i < length; i++)
                buffer[i] = (char)(seed + (i % 26));
            return new string(buffer);
        }

        public static string CreateNumericString(int length)
        {
            length = ClampLength(length);
            var buffer = new char[length];
            for (int i = 0; i < length; i++)
                buffer[i] = (char)('0' + (i % 10));
            return new string(buffer);
        }
    }
}
