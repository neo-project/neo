using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.Miner
{
    internal class BlockConsensusRequest : ISerializable
    {
        public UInt256 PrevHash;
        public Dictionary<Secp256r1Point, byte[]> NoncePieces;
        public UInt256 NonceHash;
        public UInt256[] TransactionHashes;

        [NonSerialized]
        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(this.ToArray().Sha256().Sha256());
                }
                return _hash;
            }
        }

        public BlockConsensusRequest()
        {
            this.NoncePieces = new Dictionary<Secp256r1Point, byte[]>();
        }

        public void Deserialize(BinaryReader reader)
        {
            this.PrevHash = reader.ReadSerializable<UInt256>();
            this.NoncePieces.Clear();
            int count = (int)reader.ReadVarInt();
            for (int i = 0; i < count; i++)
            {
                Secp256r1Point key = Secp256r1Point.DeserializeFrom(reader);
                byte[] value = reader.ReadBytes((int)reader.ReadVarInt());
                NoncePieces.Add(key, value);
            }
            this.NonceHash = reader.ReadSerializable<UInt256>();
            this.TransactionHashes = reader.ReadSerializableArray<UInt256>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PrevHash);
            writer.WriteVarInt(NoncePieces.Count);
            foreach (var pair in NoncePieces)
            {
                writer.Write(pair.Key);
                writer.WriteVarInt(pair.Value.Length);
                writer.Write(pair.Value);
            }
            writer.Write(NonceHash);
            writer.Write(TransactionHashes);
        }
    }
}
