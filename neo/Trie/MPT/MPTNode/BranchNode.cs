using Neo.IO;
using Neo.IO.Json;
using System.IO;
using System.Linq;

namespace Neo.Trie.MPT
{
    public class BranchNode : MPTNode
    {
        public const int ChildCount = 17;
        public MPTNode[] Children = new MPTNode[ChildCount];

        protected override NodeType Type => NodeType.BranchNode;
        public override int Size => base.Size + Children.Sum(n => n.IsEmptyNode ? 1 : 33);

        public BranchNode()
        {
            for (int i = 0; i < ChildCount; i++)
            {
                Children[i] = HashNode.EmptyNode;
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            foreach (var child in Children)
            {
                child.SerializeAsChild(writer);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            for (int i = 0; i < ChildCount; i++)
            {
                var hn = new HashNode();
                hn.Deserialize(reader);
                Children[i] = hn;
            }
            References = (uint)reader.ReadVarInt();
        }

        public override JObject ToJson()
        {
            var jarr = new JArray();
            for (int i = 0; i < ChildCount; i++)
            {
                jarr.Add(Children[i].ToJson());
            }
            return jarr;
        }
    }
}
