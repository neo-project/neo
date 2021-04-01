using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Trie.MPT
{
    public class HashNode : MPTNode
    {
        public UInt256 Hash;

        public HashNode()
        {
            nType = NodeType.HashNode;
        }

        public HashNode(UInt256 hash) : this()
        {
            Hash = hash;
        }

        protected override UInt256 GenHash()
        {
            return Hash;
        }

        public static HashNode EmptyNode()
        {
            return new HashNode(null);
        }

        public bool IsEmptyNode => Hash is null;

        public override void EncodeSpecific(BinaryWriter writer)
        {
            if (this.IsEmptyNode)
            {
                writer.WriteVarBytes(Array.Empty<byte>());
                return;
            }
            writer.WriteVarBytes(Hash.ToArray());
        }

        public override void DecodeSpecific(BinaryReader reader)
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
            if (!this.IsEmptyNode)
            {
                json["hash"] = Hash.ToString();
            }
            return json;
        }
    }
}
