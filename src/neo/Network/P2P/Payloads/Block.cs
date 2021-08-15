// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents a block.
    /// </summary>
    public sealed class Block : IEquatable<Block>, IInventory
    {
        /// <summary>
        /// The header of the block.
        /// </summary>
        public Header Header;

        /// <summary>
        /// The transaction list of the block.
        /// </summary>
        public Transaction[] Transactions;

        public UInt256 Hash => Header.Hash;

        /// <summary>
        /// The version of the block.
        /// </summary>
        public uint Version => Header.Version;

        /// <summary>
        /// The hash of the previous block.
        /// </summary>
        public UInt256 PrevHash => Header.PrevHash;

        /// <summary>
        /// The merkle root of the transactions.
        /// </summary>
        public UInt256 MerkleRoot => Header.MerkleRoot;

        /// <summary>
        /// The timestamp of the block.
        /// </summary>
        public ulong Timestamp => Header.Timestamp;

        /// <summary>
        /// The random number of the block.
        /// </summary>
        public ulong Nonce => Header.Nonce;

        /// <summary>
        /// The index of the block.
        /// </summary>
        public uint Index => Header.Index;

        /// <summary>
        /// The primary index of the consensus node that generated this block.
        /// </summary>
        public byte PrimaryIndex => Header.PrimaryIndex;

        /// <summary>
        /// The multi-signature address of the consensus nodes that generates the next block.
        /// </summary>
        public UInt160 NextConsensus => Header.NextConsensus;

        /// <summary>
        /// The witness of the block.
        /// </summary>
        public Witness Witness => Header.Witness;

        InventoryType IInventory.InventoryType => InventoryType.Block;
        public int Size => Header.Size + Transactions.GetVarSize();
        Witness[] IVerifiable.Witnesses { get => ((IVerifiable)Header).Witnesses; set => throw new NotSupportedException(); }

        public void Deserialize(BinaryReader reader)
        {
            Header = reader.ReadSerializable<Header>();
            Transactions = reader.ReadSerializableArray<Transaction>(ushort.MaxValue);
            if (Transactions.Distinct().Count() != Transactions.Length)
                throw new FormatException();
            if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != Header.MerkleRoot)
                throw new FormatException();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader) => throw new NotSupportedException();

        public bool Equals(Block other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Block);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot) => ((IVerifiable)Header).GetScriptHashesForVerifying(snapshot);

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.Write(Transactions);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer) => ((IVerifiable)Header).SerializeUnsigned(writer);

        /// <summary>
        /// Converts the block to a JSON object.
        /// </summary>
        /// <param name="settings">The <see cref="ProtocolSettings"/> used during the conversion.</param>
        /// <returns>The block represented by a JSON object.</returns>
        public JObject ToJson(ProtocolSettings settings)
        {
            JObject json = Header.ToJson(settings);
            json["size"] = Size;
            json["tx"] = Transactions.Select(p => p.ToJson(settings)).ToArray();
            return json;
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot)
        {
            return Header.Verify(settings, snapshot);
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot, HeaderCache headerCache)
        {
            return Header.Verify(settings, snapshot, headerCache);
        }
    }
}
