// Copyright (C) 2015-2025 The Neo Project.
//
// RecoveryRequestGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Utils;
using Neo.Plugins.DBFTPlugin.Messages;
using System;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Generators
{
    /// <summary>
    /// Generator for RecoveryRequest messages
    /// </summary>
    public static class RecoveryRequestGenerator
    {
        /// <summary>
        /// Generate a standard RecoveryRequest message
        /// </summary>
        public static void Generate(string outputDirectory, int index)
        {
            var recoveryRequest = new RecoveryRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"recovery_request_{index}.bin", recoveryRequest, "dbft.rr");
        }

        /// <summary>
        /// Generate a RecoveryRequest message with a future block index
        /// </summary>
        public static void GenerateWithFutureBlockIndex(string outputDirectory)
        {
            var recoveryRequest = new RecoveryRequest
            {
                BlockIndex = uint.MaxValue - 1, // Very high block index
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "recovery_request_future_block.bin", recoveryRequest, "dbft.rr");
        }

        /// <summary>
        /// Generate a RecoveryRequest message with a high view number
        /// </summary>
        public static void GenerateWithHighViewNumber(string outputDirectory)
        {
            var recoveryRequest = new RecoveryRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = byte.MaxValue, // Maximum view number
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "recovery_request_high_view.bin", recoveryRequest, "dbft.rr");
        }
        /// <summary>
        /// Generate a RecoveryRequest message with a custom validator index
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex, string filename = null)
        {
            var recoveryRequest = new RecoveryRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string outputFilename = filename ?? $"recovery_request_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, recoveryRequest, "dbft.rr");
        }

        /// <summary>
        /// Generate a RecoveryRequest message with a custom block index
        /// </summary>
        public static void GenerateWithCustomBlockIndex(string outputDirectory, uint blockIndex, string filename = null)
        {
            var recoveryRequest = new RecoveryRequest
            {
                BlockIndex = blockIndex,
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string outputFilename = filename ?? $"recovery_request_block_{blockIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, recoveryRequest, "dbft.rr");
        }
    }
}
