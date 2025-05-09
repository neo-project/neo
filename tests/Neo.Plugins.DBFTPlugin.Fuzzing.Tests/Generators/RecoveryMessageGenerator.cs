// Copyright (C) 2015-2025 The Neo Project.
//
// RecoveryMessageGenerator.cs file belongs to the neo project and is free
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
using System.Collections.Generic;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Generators
{
    /// <summary>
    /// Generator for RecoveryMessage messages
    /// </summary>
    public static class RecoveryMessageGenerator
    {
        /// <summary>
        /// Generate a standard RecoveryMessage
        /// </summary>
        public static void Generate(string outputDirectory, int index)
        {
            // Recovery messages are complex, this is a simplified version
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            };

            // Add some random payloads
            for (int i = 0; i < 7; i++)
            {
                byte validatorIndex = (byte)i;

                // 50% chance to add a ChangeViewPayloadCompact
                if (FuzzingHelpers.Random.Next(0, 2) == 0)
                {
                    recoveryMessage.ChangeViewMessages[validatorIndex] = new RecoveryMessage.ChangeViewPayloadCompact
                    {
                        ValidatorIndex = validatorIndex,
                        OriginalViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                        Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                    };
                }

                // 50% chance to add a PreparationPayloadCompact
                if (FuzzingHelpers.Random.Next(0, 2) == 0)
                {
                    recoveryMessage.PreparationMessages[validatorIndex] = new RecoveryMessage.PreparationPayloadCompact
                    {
                        ValidatorIndex = validatorIndex,
                        InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                    };
                }

                // 50% chance to add a CommitPayloadCompact
                if (FuzzingHelpers.Random.Next(0, 2) == 0)
                {
                    recoveryMessage.CommitMessages[validatorIndex] = new RecoveryMessage.CommitPayloadCompact
                    {
                        ValidatorIndex = validatorIndex,
                        ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                        Signature = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(64)),
                        InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                    };
                }
            }

            // 50% chance to add a PrepareRequestMessage
            if (FuzzingHelpers.Random.Next(0, 2) == 0)
            {
                recoveryMessage.PrepareRequestMessage = new PrepareRequest
                {
                    BlockIndex = recoveryMessage.BlockIndex,
                    ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                    ViewNumber = recoveryMessage.ViewNumber,
                    Version = 0,
                    PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = (ulong)FuzzingHelpers.Random.Next(),
                    TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(0, 3)]
                };

                // Generate random transaction hashes
                for (int i = 0; i < recoveryMessage.PrepareRequestMessage.TransactionHashes.Length; i++)
                {
                    recoveryMessage.PrepareRequestMessage.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
                }
            }
            else
            {
                // If no PrepareRequestMessage, add a PreparationHash 50% of the time
                if (FuzzingHelpers.Random.Next(0, 2) == 0)
                {
                    recoveryMessage.PreparationHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
                }
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, $"recovery_message_{index}.bin", recoveryMessage, "dbft.rm");
        }

        /// <summary>
        /// Generate a RecoveryMessage with inconsistent state
        /// </summary>
        public static void GenerateWithInconsistentState(string outputDirectory)
        {
            byte viewNumber = (byte)FuzzingHelpers.Random.Next(0, 3);
            uint blockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000);

            // Create a recovery message with inconsistent view numbers in commits
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = blockIndex,
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = viewNumber,
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            };

            // Add some commit messages with inconsistent view numbers
            for (int i = 0; i < 7; i++)
            {
                byte validatorIndex = (byte)i;

                // Alternate between correct and incorrect view numbers
                byte commitViewNumber = (i % 2 == 0) ? viewNumber : (byte)(viewNumber + 1);

                recoveryMessage.CommitMessages[validatorIndex] = new RecoveryMessage.CommitPayloadCompact
                {
                    ValidatorIndex = validatorIndex,
                    ViewNumber = commitViewNumber, // Inconsistent with recovery message view number
                    Signature = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(64)),
                    InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                };
            }

            // Add a PrepareRequestMessage with inconsistent block index
            recoveryMessage.PrepareRequestMessage = new PrepareRequest
            {
                BlockIndex = blockIndex + 1, // Inconsistent with recovery message block index
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = viewNumber,
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 3)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < recoveryMessage.PrepareRequestMessage.TransactionHashes.Length; i++)
            {
                recoveryMessage.PrepareRequestMessage.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, "recovery_message_inconsistent.bin", recoveryMessage, "dbft.rm");
        }

        /// <summary>
        /// Generate a RecoveryMessage with empty dictionaries
        /// </summary>
        public static void GenerateWithEmptyDictionaries(string outputDirectory)
        {
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
                // All dictionaries are empty
            };

            // No PrepareRequestMessage or PreparationHash

            MessageSerializer.WriteMessageToFile(outputDirectory, "recovery_message_empty_dictionaries.bin", recoveryMessage, "dbft.rm");
        }

        /// <summary>
        /// Generate a RecoveryMessage with empty dictionaries (simulating null dictionaries)
        /// </summary>
        public static void GenerateWithNullDictionaries(string outputDirectory)
        {
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                // Empty dictionaries instead of null to avoid NullReferenceException
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "recovery_message_empty_dictionaries_2.bin", recoveryMessage, "dbft.rm");
        }
        /// <summary>
        /// Generate a RecoveryMessage with a custom validator index
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex, string filename = null)
        {
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            };

            // Add some random payloads
            for (int i = 0; i < 3; i++) // Add fewer payloads for simplicity
            {
                byte otherValidatorIndex = (byte)((validatorIndex + i + 1) % 7); // Ensure different from the main validator

                // Add a ChangeViewPayloadCompact
                recoveryMessage.ChangeViewMessages[otherValidatorIndex] = new RecoveryMessage.ChangeViewPayloadCompact
                {
                    ValidatorIndex = otherValidatorIndex,
                    OriginalViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                };

                // Add a CommitPayloadCompact
                recoveryMessage.CommitMessages[otherValidatorIndex] = new RecoveryMessage.CommitPayloadCompact
                {
                    ValidatorIndex = otherValidatorIndex,
                    ViewNumber = recoveryMessage.ViewNumber,
                    Signature = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(64)),
                    InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                };
            }

            // Add a PrepareRequestMessage
            recoveryMessage.PrepareRequestMessage = new PrepareRequest
            {
                BlockIndex = recoveryMessage.BlockIndex,
                ValidatorIndex = (byte)(validatorIndex == 0 ? 0 : (validatorIndex - 1)), // Primary is usually validator 0 or one less than current
                ViewNumber = recoveryMessage.ViewNumber,
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[2]
            };

            // Generate random transaction hashes
            for (int i = 0; i < recoveryMessage.PrepareRequestMessage.TransactionHashes.Length; i++)
            {
                recoveryMessage.PrepareRequestMessage.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? $"recovery_message_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, recoveryMessage, "dbft.rm");
        }

        /// <summary>
        /// Generate a RecoveryMessage with maximum entries in all dictionaries
        /// </summary>
        public static void GenerateWithMaxEntries(string outputDirectory, string filename = null)
        {
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            };

            // Add entries for all possible validator indices (0-6)
            for (byte i = 0; i < 7; i++)
            {
                // Add a ChangeViewPayloadCompact for each validator
                recoveryMessage.ChangeViewMessages[i] = new RecoveryMessage.ChangeViewPayloadCompact
                {
                    ValidatorIndex = i,
                    OriginalViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                };

                // Add a PreparationPayloadCompact for each validator
                recoveryMessage.PreparationMessages[i] = new RecoveryMessage.PreparationPayloadCompact
                {
                    ValidatorIndex = i,
                    InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                };

                // Add a CommitPayloadCompact for each validator
                recoveryMessage.CommitMessages[i] = new RecoveryMessage.CommitPayloadCompact
                {
                    ValidatorIndex = i,
                    ViewNumber = recoveryMessage.ViewNumber,
                    Signature = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(64)),
                    InvocationScript = new ReadOnlyMemory<byte>(FuzzingHelpers.GenerateRandomBytes(10))
                };
            }

            // Add a PrepareRequestMessage
            recoveryMessage.PrepareRequestMessage = new PrepareRequest
            {
                BlockIndex = recoveryMessage.BlockIndex,
                ValidatorIndex = 0, // Primary
                ViewNumber = recoveryMessage.ViewNumber,
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[10] // Maximum number of transactions
            };

            // Generate random transaction hashes
            for (int i = 0; i < recoveryMessage.PrepareRequestMessage.TransactionHashes.Length; i++)
            {
                recoveryMessage.PrepareRequestMessage.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? "recovery_message_max_entries.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, recoveryMessage, "dbft.rm");
        }
    }
}
