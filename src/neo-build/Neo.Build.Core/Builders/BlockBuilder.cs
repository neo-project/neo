// Copyright (C) 2015-2025 The Neo Project.
//
// BlockBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Builders;
using Neo.Cryptography;
using Neo.Extensions.Factories;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;

namespace Neo.Build.Core.Builders
{
    public class BlockBuilder
    {
        private BlockBuilder() { }

        private readonly Block _block = new()
        {
            Header = new()
            {
                Nonce = RandomNumberFactory.NextUInt64(),
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                MerkleRoot = new(),
                NextConsensus = new(),
                PrevHash = new(),
                Witness = new()
                {
                    InvocationScript = Memory<byte>.Empty,
                    VerificationScript = Memory<byte>.Empty,
                },
            },
            Transactions = [],
        };

        public static BlockBuilder Create() =>
            new();

        public static BlockBuilder CreateNext(Block prevBlock) =>
            new BlockBuilder()
                .AddPrevHash(prevBlock.Hash)
                .AddIndex(prevBlock.Index + 1);

        public static BlockBuilder CreateNext(Block prevBlock, ProtocolSettings protocolSettings) =>
            new BlockBuilder()
                .AddPrevHash(prevBlock.Hash)
                .AddIndex(prevBlock.Index + 1)
                .AddTimestamp(config => config += protocolSettings.MillisecondsPerBlock);

        public BlockBuilder AddIndex(uint index)
        {
            _block.Header.Index = index;

            return this;
        }

        public BlockBuilder AddPrimaryIndex(byte index)
        {
            _block.Header.PrimaryIndex = index;

            return this;
        }

        public BlockBuilder AddNextConsensus(UInt160 hash)
        {
            _block.Header.NextConsensus = hash;

            return this;
        }

        public BlockBuilder AddNonce(ulong nonce)
        {
            _block.Header.Nonce = nonce;

            return this;
        }

        public BlockBuilder AddPrevHash(UInt256 hash)
        {
            _block.Header.PrevHash = hash;

            return this;
        }

        public BlockBuilder AddTimestamp(ulong timestamp)
        {
            _block.Header.Timestamp = timestamp;

            return this;
        }

        public BlockBuilder AddTimestamp(Action<ulong> config)
        {
            var timestamp = _block.Header.Timestamp;
            config(timestamp);
            _block.Header.Timestamp = timestamp;

            return this;
        }

        public BlockBuilder AddTimestamp(DateTimeOffset timestamp)
        {
            _block.Header.Timestamp = (ulong)timestamp.ToUnixTimeMilliseconds();

            return this;
        }

        public BlockBuilder AddVersion(uint version)
        {
            _block.Header.Version = version;

            return this;
        }

        public BlockBuilder AddWitness(Action<WitnessBuilder> config)
        {
            var wb = WitnessBuilder.CreateEmpty();
            config(wb);
            _block.Header.Witness = wb.Build();

            return this;
        }

        public BlockBuilder AddTransaction(Action<TransactionBuilder> config)
        {

            var tx = TransactionBuilder.CreateEmpty();
            config(tx);
            _block.Transactions = [.. _block.Transactions, tx.Build()];

            return this;
        }

        public Block Build()
        {
            _block.Header.MerkleRoot = MerkleTree.ComputeRoot([.. _block.Transactions.Select(static s => s.Hash)]);
            return _block;
        }
    }
}
