using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Trie.MPT
{
    public class LeafNode : MPTNode
    {
        protected override NodeType Type => NodeType.LeafNode;
        public static readonly int MaxValueLength = 1024 * 1024;
        public byte[] Value;
        public override int Size => base.Size + Value.GetVarSize();

        public LeafNode()
        {

        }

        public LeafNode(byte[] val)
        {
            Value = (byte[])val.Clone();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes(MaxValueLength);
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["value"] = Value.ToHexString();
            return json;
        }
    }
}
