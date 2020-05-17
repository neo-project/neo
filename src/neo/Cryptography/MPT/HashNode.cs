using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public class HashNode : MPTNode
    {
        public UInt256 Hash;

        protected override NodeType Type => NodeType.HashNode;
        public static HashNode EmptyNode { get; } = new HashNode();

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

        public bool IsEmpty => Hash is null;

        public override void EncodeSpecific(BinaryWriter writer)
        {
            if (this.IsEmpty)
            {
                writer.WriteVarBytes(Array.Empty<byte>());
                return;
            }
            writer.WriteVarBytes(Hash.ToArray());
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            var len = reader.ReadVarInt();
            if (len < 1)
            {
                Hash = null;
                return;
            }
            if (len != UInt256.Length) throw new InvalidOperationException("Invalid hash bytes");
            Hash = new UInt256(reader.ReadFixedBytes((int)len));
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
