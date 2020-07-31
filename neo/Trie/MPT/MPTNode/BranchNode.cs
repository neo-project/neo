using Neo.IO.Json;
using System.IO;

namespace Neo.Trie.MPT
{
    public class BranchNode : MPTNode
    {
        public const int ChildCount = 17;
        public MPTNode[] Children = new MPTNode[ChildCount];

        public BranchNode()
        {
            nType = NodeType.BranchNode;
            for (int i = 0; i < ChildCount; i++)
            {
                Children[i] = HashNode.EmptyNode();
            }
        }

        public override void EncodeSpecific(BinaryWriter writer)
        {
            for (int i = 0; i < ChildCount; i++)
            {
                var hashNode = new HashNode(Children[i].GetHash());
                hashNode.EncodeSpecific(writer);
            }
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            for (int i = 0; i < ChildCount; i++)
            {
                var hashNode = new HashNode();
                hashNode.DecodeSpecific(reader);
                Children[i] = hashNode;
            }
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
