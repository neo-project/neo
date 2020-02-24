using Neo.Cryptography;
using Neo.IO;
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

        protected override byte[] GenHash()
        {
            var bytes = new byte[0];
            for (int i = 0; i < Children.Length; i++)
            {
                bytes = bytes.Concat(Children[i].GetHash());
            }
            return Crypto.Hash256(bytes);
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
    }
}
