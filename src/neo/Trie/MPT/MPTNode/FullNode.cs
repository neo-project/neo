using Neo.Cryptography;
using Neo.IO.Json;
using System.IO;

namespace Neo.Trie.MPT
{
    public class FullNode : MPTNode
    {
        public const int CHILD_COUNT = 17;
        public MPTNode[] Children = new MPTNode[CHILD_COUNT];


        public FullNode()
        {
            nType = NodeType.FullNode;
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i] = HashNode.EmptyNode();
            }
        }

        public override void EncodeSpecific(BinaryWriter writer)
        {
            for (int i = 0; i < Children.Length; i++)
            {
                var hashNode = new HashNode(Children[i].GetHash());
                hashNode.EncodeSpecific(writer);
            }
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            for (int i = 0; i < Children.Length; i++)
            {
                var hashNode = new HashNode();
                hashNode.DecodeSpecific(reader);
                Children[i] = hashNode;
            }
        }

        public override JObject ToJson()
        {
            var jarr = new JArray();
            for (int i = 0; i < CHILD_COUNT; i++)
            {
                jarr.Add(Children[i].ToJson());
            }
            return jarr;
        }
    }
}
