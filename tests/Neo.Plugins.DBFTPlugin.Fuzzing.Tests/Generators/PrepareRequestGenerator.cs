// Copyright (C) 2015-2025 The Neo Project.
//
// PrepareRequestGenerator.cs file belongs to the neo project and is free
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
    /// Generator for PrepareRequest messages
    /// </summary>
    public static class PrepareRequestGenerator
    {
        /// <summary>
        /// Generate a standard PrepareRequest message
        /// </summary>
        public static void Generate(string outputDirectory, int index)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(0, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, $"prepare_request_{index}.bin", request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with a custom category
        /// </summary>
        public static void GenerateWithCustomCategory(string outputDirectory, string category)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(0, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, $"prepare_request_category_{category}.bin", request, category);
        }

        /// <summary>
        /// Generate a PrepareRequest message with maximum transactions
        /// </summary>
        public static void GenerateWithMaxTransactions(string outputDirectory)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[512] // A large number that might be near the limit
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_request_max_tx.bin", request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with duplicate transaction hashes
        /// </summary>
        public static void GenerateWithDuplicateTransactions(string outputDirectory)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[5]
            };

            // Generate a single transaction hash and use it multiple times
            var hash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = hash;
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_request_duplicate_tx.bin", request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with zero transactions
        /// </summary>
        public static void GenerateWithZeroTransactions(string outputDirectory)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[0] // Empty array
            };

            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_request_zero_tx.bin", request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with timestamp in the past
        /// </summary>
        public static void GenerateWithPastTimestamp(string outputDirectory)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds(), // 1 day in the past
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_request_past_timestamp.bin", request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with a custom validator index
        /// </summary>
        public static void GenerateWithCustomValidatorIndex(string outputDirectory, byte validatorIndex, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? $"prepare_request_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }

        /// <summary>
        /// Generate conflicting PrepareRequest messages from the same validator
        /// </summary>
        public static void GenerateConflictingRequests(string outputDirectory)
        {
            byte validatorIndex = 0; // Primary
            uint blockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000);
            byte viewNumber = 0;

            // First request
            var request1 = new PrepareRequest
            {
                BlockIndex = blockIndex,
                ValidatorIndex = validatorIndex,
                ViewNumber = viewNumber,
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[2]
            };

            // Generate random transaction hashes for first request
            for (int i = 0; i < request1.TransactionHashes.Length; i++)
            {
                request1.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            // Second request with same validator, block index, and view number but different transactions
            var request2 = new PrepareRequest
            {
                BlockIndex = blockIndex,
                ValidatorIndex = validatorIndex,
                ViewNumber = viewNumber,
                Version = 0,
                PrevHash = request1.PrevHash, // Same prev hash
                Timestamp = request1.Timestamp, // Same timestamp
                Nonce = request1.Nonce, // Same nonce
                TransactionHashes = new UInt256[3] // Different number of transactions
            };

            // Generate different transaction hashes for second request
            for (int i = 0; i < request2.TransactionHashes.Length; i++)
            {
                request2.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_request_conflict_1.bin", request1, "dbft.pr");
            MessageSerializer.WriteMessageToFile(outputDirectory, "prepare_request_conflict_2.bin", request2, "dbft.pr");
        }


        /// <summary>
        /// Generate a PrepareRequest message with a custom block index
        /// </summary>
        public static void GenerateWithCustomBlockIndex(string outputDirectory, uint blockIndex, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = blockIndex,
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? $"prepare_request_block_{blockIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with a custom view number
        /// </summary>
        public static void GenerateWithCustomViewNumber(string outputDirectory, byte viewNumber, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = viewNumber,
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? $"prepare_request_view_{viewNumber}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with custom view and validator index
        /// </summary>
        public static void GenerateWithCustomViewAndValidatorIndex(string outputDirectory, byte viewNumber, byte validatorIndex, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = validatorIndex,
                ViewNumber = viewNumber,
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? $"prepare_request_view_{viewNumber}_validator_{validatorIndex}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }
        /// <summary>
        /// Generate a PrepareRequest message with a custom transaction count
        /// </summary>
        public static void GenerateWithCustomTransactionCount(string outputDirectory, int transactionCount, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[transactionCount]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? $"prepare_request_tx_count_{transactionCount}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }
        /// <summary>
        /// Generate a PrepareRequest message with maximum transactions
        /// </summary>
        public static void GenerateWithMaxTransactions(string outputDirectory, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[1000] // Very large number of transactions
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? "prepare_request_max_transactions.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with zero transactions
        /// </summary>
        public static void GenerateWithZeroTransactions(string outputDirectory, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[0] // Zero transactions
            };

            string outputFilename = filename ?? "prepare_request_zero_transactions.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with duplicate transaction hashes
        /// </summary>
        public static void GenerateWithDuplicateTransactions(string outputDirectory, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[10]
            };

            // Generate a single random transaction hash
            UInt256 duplicateHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));

            // Use the same hash for all transactions
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = duplicateHash;
            }

            string outputFilename = filename ?? "prepare_request_duplicate_transactions.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with a custom category
        /// </summary>
        public static void GenerateWithCustomCategory(string outputDirectory, string category, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? $"prepare_request_category_{category}.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, category);
        }



        /// <summary>
        /// Generate a PrepareRequest message with a timestamp in the past
        /// </summary>
        public static void GenerateWithPastTimestamp(string outputDirectory, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)(DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds()), // 30 days in the past
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? "prepare_request_past_timestamp.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }

        /// <summary>
        /// Generate a PrepareRequest message with a future timestamp
        /// </summary>
        public static void GenerateWithFutureTimestamp(string outputDirectory, string filename = null)
        {
            var request = new PrepareRequest
            {
                BlockIndex = (uint)FuzzingHelpers.Random.Next(0, 1000),
                ValidatorIndex = (byte)FuzzingHelpers.Random.Next(0, 7),
                ViewNumber = (byte)FuzzingHelpers.Random.Next(0, 3),
                Version = 0,
                PrevHash = new UInt256(FuzzingHelpers.GenerateRandomBytes(32)),
                Timestamp = (ulong)(DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds()), // 30 days in the future
                Nonce = (ulong)FuzzingHelpers.Random.Next(),
                TransactionHashes = new UInt256[FuzzingHelpers.Random.Next(1, 5)]
            };

            // Generate random transaction hashes
            for (int i = 0; i < request.TransactionHashes.Length; i++)
            {
                request.TransactionHashes[i] = new UInt256(FuzzingHelpers.GenerateRandomBytes(32));
            }

            string outputFilename = filename ?? "prepare_request_future_timestamp.bin";
            MessageSerializer.WriteMessageToFile(outputDirectory, outputFilename, request, "dbft.pr");
        }
    }
}
