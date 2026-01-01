// Copyright (C) 2015-2026 The Neo Project.
//
// Header.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents the header of a block.
    /// </summary>
    public sealed class Header : IEquatable<Header>, IVerifiable
    {
        /// <summary>
        /// The version of the block.
        /// </summary>
        public uint Version { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The hash of the previous block.
        /// </summary>
        public required UInt256 PrevHash { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The merkle root of the transactions.
        /// </summary>
        public required UInt256 MerkleRoot { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The timestamp of the block.
        /// </summary>
        public ulong Timestamp { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The first eight bytes of random number generated.
        /// </summary>
        public ulong Nonce { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The index of the block.
        /// </summary>
        public uint Index { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The primary index of the consensus node that generated this block.
        /// </summary>
        public byte PrimaryIndex { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The multi-signature address of the consensus nodes that generates the next block.
        /// </summary>
        public required UInt160 NextConsensus { get; set { field = value; _hash = null; } }

        /// <summary>
        /// The witness of the block.
        /// </summary>
        public required Witness Witness;

        private UInt256? _hash = null;

        /// <inheritdoc/>
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = this.CalculateHash();
                }
                return _hash;
            }
        }

        public int Size =>
            sizeof(uint) +      // Version
            UInt256.Length +    // PrevHash
            UInt256.Length +    // MerkleRoot
            sizeof(ulong) +     // Timestamp
            sizeof(ulong) +     // Nonce
            sizeof(uint) +      // Index
            sizeof(byte) +      // PrimaryIndex
            UInt160.Length +    // NextConsensus
            (Witness is null ? 1 : 1 + Witness.Size); // Witness, cannot be null for valid header

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value, nameof(IVerifiable.Witnesses));
                if (value.Length != 1)
                    throw new ArgumentException($"Expected 1 witness, got {value.Length}.", nameof(IVerifiable.Witnesses));
                Witness = value[0];
            }
        }

        public void Deserialize(ref MemoryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(ref reader);
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            if (witnesses.Length != 1) throw new FormatException($"Expected 1 witness in Header, got {witnesses.Length}.");
            Witness = witnesses[0];
        }

        void IVerifiable.DeserializeUnsigned(ref MemoryReader reader)
        {
            _hash = null;
            Version = reader.ReadUInt32();
            if (Version > 0) throw new FormatException($"`version`({Version}) in Header must be 0");
            PrevHash = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt64();
            Nonce = reader.ReadUInt64();
            Index = reader.ReadUInt32();
            PrimaryIndex = reader.ReadByte();
            NextConsensus = reader.ReadSerializable<UInt160>();
        }

        public bool Equals(Header? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Header);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot)
        {
            if (PrevHash == UInt256.Zero) return [Witness.ScriptHash];
            var prev = NativeContract.Ledger.GetTrimmedBlock(snapshot, PrevHash)
                ?? throw new InvalidOperationException($"Block {PrevHash} was not found");
            return [prev.Header.NextConsensus];
        }

        public void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(new Witness[] { Witness });
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.Write(Index);
            writer.Write(PrimaryIndex);
            writer.Write(NextConsensus);
        }

        /// <summary>
        /// Converts the header to a JSON object.
        /// </summary>
        /// <param name="settings">The <see cref="ProtocolSettings"/> used during the conversion.</param>
        /// <returns>The header represented by a JSON object.</returns>
        public JObject ToJson(ProtocolSettings settings)
        {
            JObject json = new();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevHash.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["nonce"] = Nonce.ToString("X16");
            json["index"] = Index;
            json["primary"] = PrimaryIndex;
            json["nextconsensus"] = NextConsensus.ToAddress(settings.AddressVersion);
            json["witnesses"] = new JArray(Witness.ToJson());
            return json;
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot)
        {
            if (PrimaryIndex >= settings.ValidatorsCount)
                return false;
            TrimmedBlock? prev = NativeContract.Ledger.GetTrimmedBlock(snapshot, PrevHash);
            if (prev is null) return false;
            if (prev.Index + 1 != Index) return false;
            if (prev.Hash != PrevHash) return false;
            if (prev.Header.Timestamp >= Timestamp) return false;
            if (!this.VerifyWitnesses(settings, snapshot, 3_00000000L)) return false;
            return true;
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot, HeaderCache headerCache)
        {
            Header? prev = headerCache.Last;
            if (prev is null) return Verify(settings, snapshot);
            if (PrimaryIndex >= settings.ValidatorsCount)
                return false;
            if (prev.Hash != PrevHash) return false;
            if (prev.Index + 1 != Index) return false;
            if (prev.Timestamp >= Timestamp) return false;
            return this.VerifyWitness(settings, snapshot, prev.NextConsensus, Witness, 3_00000000L, out _);
        }

        public Header Clone()
        {
            return new Header()
            {
                Version = Version,
                PrevHash = PrevHash,
                MerkleRoot = MerkleRoot,
                Timestamp = Timestamp,
                Nonce = Nonce,
                Index = Index,
                PrimaryIndex = PrimaryIndex,
                NextConsensus = NextConsensus,
                Witness = Witness.Clone(),
                _hash = _hash
            };
        }
    }
}
