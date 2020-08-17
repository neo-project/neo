using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ConsensusData : ISerializable
    {
        public byte PrimaryIndex;
        public ulong Nonce;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Hash256(this.ToArray()));
                }
                return _hash;
            }
        }

        public int Size => sizeof(byte) + sizeof(ulong);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            PrimaryIndex = reader.ReadByte();
            if (PrimaryIndex >= ProtocolSettings.Default.ValidatorsCount)
                throw new FormatException();
            Nonce = reader.ReadUInt64();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(PrimaryIndex);
            writer.Write(Nonce);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["primary"] = PrimaryIndex;
            json["nonce"] = Nonce.ToString("x16");
            return json;
        }
    }
}
