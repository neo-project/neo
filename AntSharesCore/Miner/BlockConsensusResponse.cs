using AntShares.Core;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Network;
using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.Miner
{
    public class BlockConsensusResponse : Inventory, ISignable
    {
        public UInt256 PrevHash;
        public Secp256r1Point Miner;
        public Dictionary<Secp256r1Point, byte[]> NoncePieces = new Dictionary<Secp256r1Point, byte[]>();
        public UInt256 MerkleRoot;
        public byte[] Script;

        public override InventoryType InventoryType => InventoryType.ConsResponse;

        byte[][] ISignable.Scripts
        {
            get
            {
                return new byte[][] { Script };
            }
            set
            {
                if (value.Length != 1)
                    throw new ArgumentException();
                Script = value[0];
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            this.PrevHash = reader.ReadSerializable<UInt256>();
            this.Miner = Secp256r1Point.DeserializeFrom(reader);
            this.NoncePieces.Clear();
            int count = (int)reader.ReadVarInt();
            for (int i = 0; i < count; i++)
            {
                Secp256r1Point key = Secp256r1Point.DeserializeFrom(reader);
                byte[] value = reader.ReadBytes((int)reader.ReadVarInt());
                NoncePieces.Add(key, value);
            }
            this.MerkleRoot = reader.ReadSerializable<UInt256>();
            this.Script = reader.ReadBytes((int)reader.ReadVarInt());
        }
    }
}
