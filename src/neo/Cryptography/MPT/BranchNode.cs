using System.IO;

namespace Neo.Cryptography.MPT
{
    public class BranchNode : MPTNode
    {
        public const int ChildCount = 17;
        public readonly MPTNode[] Children = new MPTNode[ChildCount];

        protected override NodeType Type => NodeType.BranchNode;

        public BranchNode()
        {
            for (int i = 0; i < ChildCount; i++)
            {
                Children[i] = HashNode.EmptyNode;
            }
        }

        internal override void EncodeSpecific(BinaryWriter writer)
        {
            for (int i = 0; i < ChildCount; i++)
                WriteHash(writer, Children[i].Hash);
        }

        internal override void DecodeSpecific(BinaryReader reader)
        {
            for (int i = 0; i < ChildCount; i++)
            {
                Children[i] = new HashNode();
                Children[i].DecodeSpecific(reader);
            }
        }
    }
}
