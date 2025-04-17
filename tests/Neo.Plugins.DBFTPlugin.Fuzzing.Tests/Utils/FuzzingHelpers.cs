// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzingHelpers.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Utils
{
    /// <summary>
    /// Helper methods for fuzzing
    /// </summary>
    public static class FuzzingHelpers
    {
        /// <summary>
        /// Shared random instance for deterministic generation
        /// </summary>
        public static readonly Random Random = new Random(42); // Fixed seed for reproducibility

        /// <summary>
        /// Generate random bytes
        /// </summary>
        /// <param name="length">Length of the byte array</param>
        /// <returns>Random byte array</returns>
        public static byte[] GenerateRandomBytes(int length)
        {
            byte[] buffer = new byte[length];
            Random.NextBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Generate a random byte array with a specific pattern
        /// </summary>
        /// <param name="length">Length of the byte array</param>
        /// <param name="pattern">Pattern type</param>
        /// <returns>Byte array with the specified pattern</returns>
        public static byte[] GeneratePatternBytes(int length, PatternType pattern)
        {
            byte[] buffer = new byte[length];

            switch (pattern)
            {
                case PatternType.AllZeros:
                    // Leave as zeros
                    break;

                case PatternType.AllOnes:
                    for (int i = 0; i < length; i++)
                        buffer[i] = 0xFF;
                    break;

                case PatternType.Alternating:
                    for (int i = 0; i < length; i++)
                        buffer[i] = (byte)(i % 2 == 0 ? 0x55 : 0xAA);
                    break;

                case PatternType.Incrementing:
                    for (int i = 0; i < length; i++)
                        buffer[i] = (byte)(i % 256);
                    break;

                case PatternType.Decrementing:
                    for (int i = 0; i < length; i++)
                        buffer[i] = (byte)(255 - (i % 256));
                    break;

                default:
                    Random.NextBytes(buffer);
                    break;
            }

            return buffer;
        }
    }

    /// <summary>
    /// Types of patterns for generating test data
    /// </summary>
    public enum PatternType
    {
        Random,
        AllZeros,
        AllOnes,
        Alternating,
        Incrementing,
        Decrementing
    }
}
