// Copyright (C) 2015-2025 The Neo Project.
//
// CommitGenerator.cs file belongs to the neo project and is free
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
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Generators
{
    /// <summary>
    /// Generator for Commit messages
    /// </summary>
    public static class CommitGenerator
    {
        /// <summary>
        /// Generate a standard Commit message
        /// </summary>
        public static void Generate(string outputDirectory, int index)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"commit_{index}.bin", commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with a custom validator index
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"commit_validator_{validatorIndex}.bin", commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with an invalid signature
        /// </summary>
        public static void GenerateWithInvalidSignature(string outputDirectory)
        {
            // Create a commit with a signature that's not the right length
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(63) // Invalid length (should be 64)
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "commit_invalid_signature.bin", commit, "dbft.commit");

            // Create a commit with a signature that's all zeros
            var commitZeros = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = new byte[64] // All zeros
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "commit_zero_signature.bin", commitZeros, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with a custom block index
        /// </summary>
        public static void GenerateWithCustomBlockIndex(string outputDirectory, uint blockIndex, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = blockIndex,
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            string outputFilename = filename ?? $"commit_block_{blockIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with a future timestamp
        /// </summary>
        public static void GenerateWithFutureTimestamp(string outputDirectory)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "commit_future_timestamp.bin", commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with a mismatched view number
        /// </summary>
        public static void GenerateWithMismatchedView(string outputDirectory)
        {
            // Generate a set of commits with different view numbers
            // This simulates Byzantine behavior where a validator sends commits for different views

            uint blockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000);
            byte validatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7);

            for (byte viewNumber = 0; viewNumber < 3; viewNumber++)
            {
                var commit = new Commit
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = validatorIndex,
                    ViewNumber = viewNumber,
                    Signature = FuzzingHelpers.GenerateRandomBytes(64)
                };

                MessageSerializer.WriteMessageToFile(outputDirectory, $"commit_mismatched_view_{viewNumber}.bin", commit, "dbft.commit");
            }
        }

        /// <summary>
        /// Generate a Commit message with a custom validator index and filename
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            string outputFilename = filename ?? $"commit_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }
        /// <summary>
        /// Generate a Commit message with a custom view number
        /// </summary>
        public static void GenerateWithCustomViewNumber(string outputDirectory, byte viewNumber, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = viewNumber,
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            string outputFilename = filename ?? $"commit_view_{viewNumber}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with custom view and validator index
        /// </summary>
        public static void GenerateWithCustomViewAndValidatorIndex(string outputDirectory, byte viewNumber, byte validatorIndex, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = viewNumber,
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            string outputFilename = filename ?? $"commit_view_{viewNumber}_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }
        /// <summary>
        /// Generate a Commit message with all zero signature
        /// </summary>
        public static void GenerateWithAllZeroSignature(string outputDirectory, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = new byte[64] // All zeros
            };

            string outputFilename = filename ?? "commit_all_zero_signature.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with all one signature
        /// </summary>
        public static void GenerateWithAllOneSignature(string outputDirectory, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = Enumerable.Repeat<byte>(0xFF, 64).ToArray() // All ones
            };

            string outputFilename = filename ?? "commit_all_one_signature.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with a custom timestamp
        /// </summary>
        public static void GenerateWithCustomTimestamp(string outputDirectory, ulong timestamp, string filename = null)
        {
            // Note: Commit messages don't actually have a timestamp field in the DBFT protocol
            // This method is included for completeness, but it just generates a regular commit
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            string outputFilename = filename ?? $"commit_timestamp_{timestamp}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }
        /// <summary>
        /// Generate a Commit message with an invalid signature
        /// </summary>
        public static void GenerateWithInvalidSignature(string outputDirectory, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(32) // Invalid signature length (should be 64)
            };

            string outputFilename = filename ?? "commit_invalid_signature.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with a future timestamp
        /// </summary>
        public static void GenerateWithFutureTimestamp(string outputDirectory, string filename = null)
        {
            // Note: Commit messages don't actually have a timestamp field in the DBFT protocol
            // This method is included for completeness, but it just generates a regular commit
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            string outputFilename = filename ?? "commit_future_timestamp.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }

        /// <summary>
        /// Generate a Commit message with a mismatched view
        /// </summary>
        public static void GenerateWithMismatchedView(string outputDirectory, string filename = null)
        {
            var commit = new Commit
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = 255, // Very high view number that won't match others
                Signature = FuzzingHelpers.GenerateRandomBytes(64)
            };

            string outputFilename = filename ?? "commit_mismatched_view.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, commit, "dbft.commit");
        }
    }
}
