using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Trie.MPT
{
    public class ExtensionNode : MPTNode
    {
        protected override NodeType Type => NodeType.ExtensionNode;
        //Max StorageKey length
        public const int MaxKeyLength = 1125;
        public byte[] Key;
        public MPTNode Next;
        public override int Size => base.Size + Key.GetVarSize() + (Next.IsEmptyNode ? 1 : 33);

        public ExtensionNode()
        {

        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Key);
            Next.SerializeAsChild(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Key = reader.ReadVarBytes(MaxKeyLength);
            var hn = new HashNode();
            hn.Deserialize(reader);
            Next = hn;
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["key"] = Key.ToHexString();
            json["next"] = Next.ToJson();
            return json;
        }
    }
}
