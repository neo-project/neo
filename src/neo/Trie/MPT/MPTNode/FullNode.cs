using System.IO;
using Neo.Cryptography;
using Neo.IO;

namespace Neo.Trie.MPT
{
    public class FullNode : MPTNode
    {
        public MPTNode[] Children = new MPTNode[17];

        public new int Size;

        public FullNode()
        {
            nType = NodeType.FullNode;
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i] = HashNode.EmptyNode();
            }
        }

        protected override byte[] calHash()
        {
            var bytes = new byte[0];
            for (int i = 0; i < Children.Length; i++)
            {
                bytes = bytes.Concat(Children[i].GetHash());
            }
            return bytes.Sha256();
        }

        public FullNode Clone()
        {
            var cloned = new FullNode();
            for (int i = 0; i < Children.Length; i++)
            {
                cloned.Children[i] = Children[i];
            }
            return cloned;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            for (int i = 0; i < Children.Length; i++)
            {
                writer.WriteVarBytes(Children[i].GetHash());
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            for (int i = 0; i < Children.Length; i++)
            {
                var hashNode = new HashNode(reader.ReadVarBytes());
                Children[i] = hashNode;
            }
        }
    }
}