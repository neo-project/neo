// Copyright (C) 2015-2025 The Neo Project.
//
// PrepareResponseGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Utils;
using Neo.Plugins.DBFTPlugin.Messages;
using System;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Generators
{
    /// <summary>
    /// Generator for PrepareResponse messages
    /// </summary>
    public static class PrepareResponseGenerator
    {
        /// <summary>
        /// Generate a standard PrepareResponse message
        /// </summary>
        public static void Generate(string outputDirectory, int index)
        {
            var response = new PrepareResponse
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32))
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"prepare_response_{index}.bin", response, "dbft.ps");
        }

        /// <summary>
        /// Generate a PrepareResponse message with a zero hash
        /// </summary>
        public static void GenerateWithZeroHash(string outputDirectory)
        {
            var response = new PrepareResponse
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                PreparationHash = UInt256.Zero
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_response_zero_hash.bin", response, "dbft.ps");
        }

        /// <summary>
        /// Generate a PrepareResponse message with a specific view number
        /// </summary>
        public static void GenerateWithCustomViewNumber(string outputDirectory, byte viewNumber, string filename = null)
        {
            var response = new PrepareResponse
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = viewNumber,
                PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32))
            };

            string outputFilename = filename ?? $"prepare_response_view_{viewNumber}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, response, "dbft.ps");
        }

        /// <summary>
        /// Generate a PrepareResponse message with a custom validator index
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex, string filename = null)
        {
            var response = new PrepareResponse
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32))
            };

            string outputFilename = filename ?? $"prepare_response_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, response, "dbft.ps");
        }

        /// <summary>
        /// Generate a PrepareResponse message with an invalid hash (not matching any PrepareRequest)
        /// </summary>
        public static void GenerateWithInvalidHash(string outputDirectory)
        {
            var response = new PrepareResponse
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)) // Random hash that won't match any PrepareRequest
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_response_invalid_hash.bin", response, "dbft.ps");
        }
        /// <summary>
        /// Generate a PrepareResponse message with a custom block index
        /// </summary>
        public static void GenerateWithCustomBlockIndex(string outputDirectory, uint blockIndex, string filename = null)
        {
            var response = new PrepareResponse
            {
                BlockIndex = blockIndex,
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32))
            };

            string outputFilename = filename ?? $"prepare_response_block_{blockIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, response, "dbft.ps");
        }

        /// <summary>
        /// Generate a PrepareResponse message with custom view and validator index
        /// </summary>
        public static void GenerateWithCustomViewAndValidatorIndex(string outputDirectory, byte viewNumber, byte validatorIndex, string filename = null)
        {
            var response = new PrepareResponse
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = viewNumber,
                PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32))
            };

            string outputFilename = filename ?? $"prepare_response_view_{viewNumber}_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, response, "dbft.ps");
        }

        /// <summary>
        /// Generate a PrepareResponse message with custom block and validator index
        /// </summary>
        public static void GenerateWithCustomBlockAndValidatorIndex(string outputDirectory, uint blockIndex, byte validatorIndex, string filename = null)
        {
            var response = new PrepareResponse
            {
                BlockIndex = blockIndex,
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32))
            };

            string outputFilename = filename ?? $"prepare_response_block_{blockIndex}_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, response, "dbft.ps");
        }
    }
}
