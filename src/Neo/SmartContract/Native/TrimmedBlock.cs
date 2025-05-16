// Copyright (C) 2015-2025 The Neo Project.
//
// TrimmedBlock.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents a block which the transactions are trimmed.
    /// </summary>
    public class TrimmedBlock : IInteroperable, ISerializable
    {
        /// <summary>
        /// The header of the block.
        /// </summary>
        public Header Header;

        /// <summary>
        /// The hashes of the transactions of the block.
        /// </summary>
        public UInt256[] Hashes;

        /// <summary>
        /// The hash of the block.
        /// </summary>
        public UInt256 Hash => Header.Hash;

        /// <summary>
        /// The index of the block.
        /// </summary>
        public uint Index => Header.Index;

        public int Size => Header.Size + Hashes.GetVarSize();

        /// <summary>
        /// Create Trimmed block
        /// </summary>
        /// <param name="block">Block</param>
        /// <returns></returns>
        public static TrimmedBlock Create(Block block)
        {
            return Create(block.Header, block.Transactions.Select(p => p.Hash).ToArray());
        }

        /// <summary>
        /// Create Trimmed block
        /// </summary>
        /// <param name="header">Block header</param>
        /// <param name="txHashes">Transaction hashes</param>
        /// <returns></returns>
        public static TrimmedBlock Create(Header header, UInt256[] txHashes)
        {
            return new TrimmedBlock
            {
                Header = header,
                Hashes = txHashes
            };
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Header = reader.ReadSerializable<Header>();
            Hashes = reader.ReadSerializableArray<UInt256>(ushort.MaxValue);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.Write(Hashes);
        }

        IInteroperable IInteroperable.Clone()
        {
            // FromStackItem is not supported so we need to do the copy

            return new TrimmedBlock
            {
                Header = Header.Clone(),
                Hashes = [.. Hashes]
            };
        }

        void IInteroperable.FromReplica(IInteroperable replica)
        {
            var from = (TrimmedBlock)replica;
            Header = from.Header;
            Hashes = from.Hashes;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        StackItem IInteroperable.ToStackItem(IReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter,
            [
                // Computed properties
                Header.Hash.ToArray(),

                // BlockBase properties
                Header.Version,
                Header.PrevHash.ToArray(),
                Header.MerkleRoot.ToArray(),
                Header.Timestamp,
                Header.Nonce,
                Header.Index,
                Header.PrimaryIndex,
                Header.NextConsensus.ToArray(),

                // Block properties
                Hashes.Length
            ]);
        }
    }
}
