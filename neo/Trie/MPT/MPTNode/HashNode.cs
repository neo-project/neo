using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Trie.MPT
{
    public class HashNode : MPTNode
    {
        public UInt256 Hash;

        protected override NodeType Type => NodeType.HashNode;
        public override int Size => base.Size + (this.IsEmptyNode ? 1 : 33);


        public HashNode()
        {
        }

        public HashNode(UInt256 hash)
        {
            Hash = hash;
        }

        protected override UInt256 GenHash()
        {
            return Hash;
        }

        private void SerializeHash(BinaryWriter writer)
        {
            if (IsEmptyNode)
            {
                writer.WriteVarBytes(Array.Empty<byte>());
                return;
            }
            writer.WriteVarBytes(Hash.ToArray());
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            SerializeHash(writer);
        }

        public override void SerializeAsChild(BinaryWriter writer)
        {
            SerializeHash(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            var len = reader.ReadVarInt();
            if (len == 0)
            {
                Hash = null;
                return;
            }
            if (len != 32) throw new System.InvalidOperationException("Invalid hash bytes");
            Hash = new UInt256(reader.ReadBytes((int)len));
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            if (!IsEmptyNode)
            {
                json["hash"] = Hash.ToString();
            }
            return json;
        }
    }
}
