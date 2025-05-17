// Copyright (C) 2015-2025 The Neo Project.
//
// ChangeViewGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Utils;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using System;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Generators
{
    /// <summary>
    /// Generator for ChangeView messages
    /// </summary>
    public static class ChangeViewGenerator
    {
        /// <summary>
        /// Generate a standard ChangeView message
        /// </summary>
        public static void Generate(string outputDirectory, int index)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Reason = (ChangeViewReason)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                // NewViewNumber is a read-only property, removed assignment
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"change_view_{index}.bin", changeView, "dbft.cv");
        }

        /// <summary>
        /// Generate a ChangeView message with a custom view number
        /// </summary>
        public static void GenerateWithCustomViewNumber(string outputDirectory, byte viewNumber)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = viewNumber,
                Reason = (ChangeViewReason)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"change_view_viewnum_{viewNumber}.bin", changeView, "dbft.cv");
        }

        /// <summary>
        /// Generate a ChangeView message with a custom reason
        /// </summary>
        public static void GenerateWithCustomReason(string outputDirectory, ChangeViewReason reason)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Reason = reason,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"change_view_reason_{reason}.bin", changeView, "dbft.cv");
        }

        /// <summary>
        /// Generate a ChangeView message with a future timestamp
        /// </summary>
        public static void GenerateWithFutureTimestamp(string outputDirectory)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Reason = (ChangeViewReason)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 60000) // 1 minute in the future
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "change_view_future_timestamp.bin", changeView, "dbft.cv");
        }

        /// <summary>
        /// Generate a ChangeView message with a custom validator index
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Reason = (ChangeViewReason)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, $"change_view_validator_{validatorIndex}.bin", changeView, "dbft.cv");
        }

        /// <summary>
        /// Generate consecutive ChangeView messages for the same validator
        /// </summary>
        public static void GenerateConsecutiveViewChanges(string outputDirectory)
        {
            byte validatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7);
            uint blockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000);

            // Generate a sequence of ChangeView messages with increasing view numbers
            for (byte viewNumber = 0; viewNumber < 5; viewNumber++)
            {
                var changeView = new ChangeView
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = validatorIndex,
                    ViewNumber = viewNumber,
                    Reason = ChangeViewReason.Timeout,
                    Timestamp = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + viewNumber * 1000) // Increasing timestamps
                };

                MessageSerializer.WriteMessageToFile(outputDirectory, $"change_view_consecutive_{viewNumber}.bin", changeView, "dbft.cv");
            }
        }
        /// <summary>
        /// Generate a ChangeView message with a custom validator index and filename
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex, string filename = null)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Reason = (ChangeViewReason)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string outputFilename = filename ?? $"change_view_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, changeView, "dbft.cv");
        }

        /// <summary>
        /// Generate a ChangeView message with custom view and validator index
        /// </summary>
        public static void GenerateWithCustomViewAndValidatorIndex(string outputDirectory, byte viewNumber, byte validatorIndex, string filename = null)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = viewNumber,
                Reason = (ChangeViewReason)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string outputFilename = filename ?? $"change_view_view_{viewNumber}_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, changeView, "dbft.cv");
        }
        /// <summary>
        /// Generate a ChangeView message with an invalid reason
        /// </summary>
        public static void GenerateWithInvalidReason(string outputDirectory, string filename = null)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Reason = (ChangeViewReason)100, // Invalid reason code
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string outputFilename = filename ?? "change_view_invalid_reason.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, changeView, "dbft.cv");
        }

        /// <summary>
        /// Generate a ChangeView message with a custom view number
        /// </summary>
        public static void GenerateWithCustomViewNumber(string outputDirectory, byte viewNumber, string filename = null)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = viewNumber,
                Reason = (ChangeViewReason)FuzzingHelpers.Random.Next(0, 3),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string outputFilename = filename ?? $"change_view_view_{viewNumber}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, changeView, "dbft.cv");
        }
        /// <summary>
        /// Generate a ChangeView message with a custom reason
        /// </summary>
        public static void GenerateWithCustomReason(string outputDirectory, Types.ChangeViewReason reason, string filename = null)
        {
            var changeView = new ChangeView
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Reason = reason,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string outputFilename = filename ?? $"change_view_reason_{reason}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, changeView, "dbft.cv");
        }
    }
}
