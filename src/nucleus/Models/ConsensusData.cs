using System;
using System.Globalization;
using System.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class ConsensusData : ISerializable
    {
        private byte primaryIndex;
        private ulong nonce;

        private Lazy<UInt256> hash;
        public UInt256 Hash
        {
            get
            {
                hash ??= new Lazy<UInt256>(() => new UInt256(Crypto.Hash256(this.ToArray())));
                return hash.Value;
            }
        }

        public int Size => sizeof(byte) + sizeof(ulong);

        public byte PrimaryIndex { get => primaryIndex; set { primaryIndex = value; hash = null; } }
        public ulong Nonce { get => nonce; set { nonce = value; hash = null; } }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            PrimaryIndex = reader.ReadByte();
            Nonce = reader.ReadUInt64();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(PrimaryIndex);
            writer.Write(Nonce);
        }

        public JObject ToJson()
        {
            return new JObject()
            {
                ["primary"] = PrimaryIndex,
                ["nonce"] = Nonce.ToString("x16")
            };
        }

        public static ConsensusData FromJson(JObject json)
        {
            return new ConsensusData()
            {
                PrimaryIndex = (byte)json["primary"].AsNumber(),
                Nonce = ulong.Parse(json["nonce"].AsString(), NumberStyles.HexNumber)
            };
        }
    }
}
