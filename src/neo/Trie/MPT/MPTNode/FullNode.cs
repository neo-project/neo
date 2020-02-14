using Neo.Cryptography;
using Neo.IO;
using System.IO;

namespace Neo.Trie.MPT
{
    public class FullNode : MPTNode
    {
        public const int CHILD_COUNT = 17;

        public MPTNode[] Children = new MPTNode[CHILD_COUNT];

        public override int Size
        {
            get
            {
                var size = 1;
                for (int i = 0; i < Children.Length; i++)
                {
                    size += Children[i].GetHash().Length;
                }
                return size;
            }
        }
        
        public FullNode()
        {
            nType = NodeType.FullNode;
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i] = HashNode.EmptyNode();
            }
        }

        protected override byte[] CalHash()
        {
            var bytes = new byte[0];
            for (int i = 0; i < Children.Length; i++)
            {
                bytes = bytes.Concat(Children[i].GetHash());
            }
            return bytes.Sha256();
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
