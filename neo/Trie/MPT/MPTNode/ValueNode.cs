using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Trie.MPT
{
    public class ValueNode : MPTNode
    {
        public byte[] Value;

        protected override byte[] GenHash()
        {
            var data = this.Encode();
            return data.Length < 32 ? (byte[])data.Clone() : Crypto.Default.Hash256(data);
        }

        public ValueNode()
        {
            nType = NodeType.ValueNode;
        }

        public ValueNode(byte[] val)
        {
            nType = NodeType.ValueNode;
            Value = (byte[])val.Clone();
        }

        public override void EncodeSpecific(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["value"] = Value.ToHexString();
            return json;
        }
    }
}
