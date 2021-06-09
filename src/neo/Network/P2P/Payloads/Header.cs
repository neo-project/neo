using Neo.IO;
using Neo.IO.Json;
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
        private uint version;
        private UInt256 prevHash;
        private UInt256 merkleRoot;
        private ulong timestamp;
        private ulong nonce;
        private uint index;
        private byte primaryIndex;
        private UInt160 nextConsensus;

        /// <summary>
        /// The witness of the block.
        /// </summary>
        public Witness Witness;

        /// <summary>
        /// The version of the block.
        /// </summary>
        public uint Version
        {
            get => version;
            set { version = value; _hash = null; }
        }

        /// <summary>
        /// The hash of the previous block.
        /// </summary>
        public UInt256 PrevHash
        {
            get => prevHash;
            set { prevHash = value; _hash = null; }
        }

        /// <summary>
        /// The merkle root of the transactions.
        /// </summary>
        public UInt256 MerkleRoot
        {
            get => merkleRoot;
            set { merkleRoot = value; _hash = null; }
        }

        /// <summary>
        /// The timestamp of the block.
        /// </summary>
        public ulong Timestamp
        {
            get => timestamp;
            set { timestamp = value; _hash = null; }
        }

        /// <summary>
        /// The first eight bytes of random number generated from VRF.
        /// </summary>
        public ulong Nonce
        {
            get => nonce;
            set { nonce = value; _hash = null; }
        }

        /// <summary>
        /// The index of the block.
        /// </summary>
        public uint Index
        {
            get => index;
            set { index = value; _hash = null; }
        }

        /// <summary>
        /// The primary index of the consensus node that generated this block.
        /// </summary>
        public byte PrimaryIndex
        {
            get => primaryIndex;
            set { primaryIndex = value; _hash = null; }
        }

        /// <summary>
        /// The multi-signature address of the consensus nodes that generates the next block.
        /// </summary>
        public UInt160 NextConsensus
        {
            get => nextConsensus;
            set { nextConsensus = value; _hash = null; }
        }

        private UInt256 _hash = null;
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
            sizeof(ulong) +      // Nonce
            sizeof(uint) +      // Index
            sizeof(byte) +      // PrimaryIndex
            UInt160.Length +    // NextConsensus
            1 + Witness.Size;   // Witness   

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException(null, nameof(value));
                Witness = value[0];
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            if (witnesses.Length != 1) throw new FormatException();
            Witness = witnesses[0];
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            _hash = null;
            version = reader.ReadUInt32();
            if (version > 0) throw new FormatException();
            prevHash = reader.ReadSerializable<UInt256>();
            merkleRoot = reader.ReadSerializable<UInt256>();
            timestamp = reader.ReadUInt64();
            nonce = reader.ReadUInt64();
            index = reader.ReadUInt32();
            primaryIndex = reader.ReadByte();
            nextConsensus = reader.ReadSerializable<UInt160>();
        }

        public bool Equals(Header other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Header);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot)
        {
            if (prevHash == UInt256.Zero) return new[] { Witness.ScriptHash };
            TrimmedBlock prev = NativeContract.Ledger.GetTrimmedBlock(snapshot, prevHash);
            if (prev is null) throw new InvalidOperationException();
            return new[] { prev.Header.nextConsensus };
        }

        public void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(new Witness[] { Witness });
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(version);
            writer.Write(prevHash);
            writer.Write(merkleRoot);
            writer.Write(timestamp);
            writer.Write(nonce);
            writer.Write(index);
            writer.Write(primaryIndex);
            writer.Write(nextConsensus);
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
            json["version"] = version;
            json["previousblockhash"] = prevHash.ToString();
            json["merkleroot"] = merkleRoot.ToString();
            json["time"] = timestamp;
            json["nonce"] = nonce.ToString("X");
            json["index"] = index;
            json["primary"] = primaryIndex;
            json["nextconsensus"] = nextConsensus.ToAddress(settings.AddressVersion);
            json["witnesses"] = new JArray(Witness.ToJson());
            return json;
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot)
        {
            if (primaryIndex >= settings.ValidatorsCount)
                return false;
            TrimmedBlock prev = NativeContract.Ledger.GetTrimmedBlock(snapshot, prevHash);
            if (prev is null) return false;
            if (prev.Index + 1 != index) return false;
            if (prev.Header.timestamp >= timestamp) return false;
            if (!this.VerifyWitnesses(settings, snapshot, 1_00000000)) return false;
            return true;
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot, HeaderCache headerCache)
        {
            Header prev = headerCache.Last;
            if (prev is null) return Verify(settings, snapshot);
            if (primaryIndex >= settings.ValidatorsCount)
                return false;
            if (prev.Hash != prevHash) return false;
            if (prev.index + 1 != index) return false;
            if (prev.timestamp >= timestamp) return false;
            return this.VerifyWitness(settings, snapshot, prev.nextConsensus, Witness, 1_00000000, out _);
        }
    }
}
