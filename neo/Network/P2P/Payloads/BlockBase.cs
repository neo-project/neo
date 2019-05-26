using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public abstract class BlockBase : IVerifiable
    {
        public uint Version;
        public UInt256 PrevHash;
        public UInt256 MerkleRoot;
        public uint Timestamp;
        public uint Index;
        public UInt160 NextConsensus;
        public ECPoint[] Validators;
        public byte[][] Signatures;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        public virtual int Size =>
            sizeof(uint) +              //Version
            PrevHash.Size +             //PrevHash
            MerkleRoot.Size +           //MerkleRoot
            sizeof(uint) +              //Timestamp
            sizeof(uint) +              //Index
            NextConsensus.Size +        //NextConsensus
            Validators.GetVarSize() +   //Validators
            64 * Signatures.Length;     //Witness

        public virtual void Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Validators = reader.ReadSerializableArray<ECPoint>(Blockchain.MaxValidators);
            Signatures = new byte[Validators.Length - (Validators.Length - 1) / 3][];
            for (int i = 0; i < Signatures.Length; i++)
                Signatures[i] = reader.ReadBytes(64);
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt32();
            Index = reader.ReadUInt32();
            NextConsensus = reader.ReadSerializable<UInt160>();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Validators);
            foreach (byte[] signature in Signatures)
                writer.Write(signature);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Index);
            writer.Write(NextConsensus);
        }

        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevHash.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["index"] = Index;
            json["nextconsensus"] = NextConsensus.ToAddress();
            json["validators"] = Validators.Select(p => (JObject)p.ToString()).ToArray();
            json["signatures"] = Signatures.Select(p => (JObject)p.ToHexString()).ToArray();
            return json;
        }

        public bool Verify(Snapshot snapshot)
        {
            Header prev_header = snapshot.GetHeader(PrevHash);
            if (prev_header == null) return false;
            if (prev_header.Index + 1 != Index) return false;
            if (prev_header.Timestamp >= Timestamp) return false;
            if (!Contract.CreateMultiSigRedeemScript(Validators.Length - (Validators.Length - 1) / 3, Validators).ToScriptHash().Equals(prev_header.NextConsensus))
                return false;
            byte[] message = this.GetHashData();
            try
            {
                for (int i = 0, j = 0; i < Signatures.Length && j < Validators.Length;)
                {
                    if (Crypto.Default.VerifySignature(message, Signatures[i], Validators[j].EncodePoint(false)))
                        i++;
                    j++;
                    if (Signatures.Length - i > Validators.Length - j)
                        return false;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }
}
