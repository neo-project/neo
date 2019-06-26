using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using System.Globalization;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ConsensusData : ISerializable
    {
        public uint PrimaryIndex;
        public ulong Nonce;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.ToArray()));
                }
                return _hash;
            }
        }

        public int Size => IO.Helper.GetVarSize((int)PrimaryIndex) + sizeof(ulong);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            PrimaryIndex = (uint)reader.ReadVarInt(Blockchain.MaxValidators - 1);
            Nonce = reader.ReadUInt64();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(PrimaryIndex);
            writer.Write(Nonce);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["primary"] = PrimaryIndex;
            json["nonce"] = Nonce.ToString("x16");
            return json;
        }

        public static ConsensusData FromJson(JObject json)
        {
            ConsensusData block = new ConsensusData();
            block.PrimaryIndex = (uint)json["primary"].AsNumber();
            block.Nonce = ulong.Parse(json["nonce"].AsString(), NumberStyles.HexNumber);
            return block;
        }

    }
}
