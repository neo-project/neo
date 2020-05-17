using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public class HashNode : MPTNode
    {
        private UInt256 hash;

        public override UInt256 Hash => hash;
        protected override NodeType Type => NodeType.HashNode;
        public static HashNode EmptyNode { get; } = new HashNode();

        public HashNode()
        {
        }

        public HashNode(UInt256 hash)
        {
            this.hash = hash;
        }

        public bool IsEmpty => Hash is null;

        public override void EncodeSpecific(BinaryWriter writer)
        {
            WriteHash(writer, hash);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            byte[] buffer = reader.ReadVarBytes(UInt256.Length);
            hash = buffer.Length switch
            {
                0 => null,
                UInt256.Length => new UInt256(buffer),
                _ => throw new FormatException()
            };
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            if (!this.IsEmpty)
            {
                json["hash"] = Hash.ToString();
            }
            return json;
        }
    }
}
