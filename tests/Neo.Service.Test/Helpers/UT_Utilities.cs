// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Utilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Cryptography;
using Neo.Network.P2P.Payloads;
using Neo.Service.IO;
using Neo.VM;
using System;
using System.Security.Cryptography;
using Xunit.Abstractions;

namespace Neo.Service.Tests.Helpers
{
    internal static class UT_Utilities
    {
        public static ILoggerFactory CreateLogFactory(ITestOutputHelper outputHelper) =>
            LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddProvider(new UT_XUnitLoggerProvider(outputHelper));
            });

        public static BlockchainArchiveFile CreateNewBlockArchiveFile(string fileName)
        {
            // Dispose to force save of the manifest file in archive
            using (var archFile = new BlockchainArchiveFile(fileName, ProtocolSettings.Default.Network))
                archFile.Write(CreateRandomFilledBlock(1u));
            // Reopens archive file to read saved manifest
            return new BlockchainArchiveFile(fileName, ProtocolSettings.Default.Network);
        }

        public static Block CreateRandomFilledBlock(uint blockIndex) =>
            new()
            {
                Header = new Header
                {
                    PrevHash = new UInt256(RandomNumberGenerator.GetBytes(UInt256.Length)),
                    MerkleRoot = MerkleTree.ComputeRoot(Array.Empty<UInt256>()),
                    Timestamp = DateTime.UtcNow.ToTimestampMS(),
                    Nonce = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong))),
                    Index = blockIndex,
                    PrimaryIndex = unchecked((byte)RandomNumberGenerator.GetInt32(sizeof(byte))),
                    NextConsensus = new UInt160(RandomNumberGenerator.GetBytes(UInt160.Length)),
                    Witness = new Witness
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = new[] { (byte)OpCode.PUSH1 }
                    },
                },
                Transactions = Array.Empty<Transaction>()
            };

        public static Block CreateRandomFilledBlock() =>
            CreateRandomFilledBlock(BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(sizeof(uint))));
    }
}
