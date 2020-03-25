using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Trie.MPT
{
    public class HashNode : MPTNode
    {
        public byte[] Hash;

        public HashNode()
        {
            nType = NodeType.HashNode;
        }

        public HashNode(byte[] hash) : this()
        {
            Hash = (byte[])hash.Clone();
        }

        protected override byte[] GenHash()
        {
            if (IsEmptyNode) return Array.Empty<byte>();
            return (byte[])Hash.Clone();
        }

        public static HashNode EmptyNode()
        {
            return new HashNode(Array.Empty<byte>());
        }

        public bool IsEmptyNode => Hash is null || Hash.Length == 0;

        public override void EncodeSpecific(BinaryWriter writer)
        {
            writer.WriteVarBytes(Hash);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            Hash = reader.ReadVarBytes();
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["hash"] = Hash.ToHexString();
            return json;
        }
    }
}
