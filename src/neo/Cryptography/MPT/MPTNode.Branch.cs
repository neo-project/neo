using Neo.IO;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public partial class MPTNode : ICloneable<MPTNode>, ISerializable
    {
        public const int BranchChildCount = 17;
        public MPTNode[] Children;

        public static MPTNode NewBranch()
        {
            var n = new MPTNode
            {
                type = NodeType.BranchNode,
                Reference = 1,
                Children = new MPTNode[BranchChildCount],
            };
            for (int i = 0; i < BranchChildCount; i++)
            {
                n.Children[i] = new MPTNode();
            }
            return n;
        }

        protected int BranchSize
        {
            get
            {
                int size = 0;
                for (int i = 0; i < BranchChildCount; i++)
                {
                    size += Children[i].SizeAsChild;
                }
                return size;
            }
        }

        private void SerializeBranch(BinaryWriter writer)
        {
            for (int i = 0; i < BranchChildCount; i++)
            {
                Children[i].SerializeAsChild(writer);
            }
        }

        private void DeserializeBranch(BinaryReader reader)
        {
            Children = new MPTNode[BranchChildCount];
            for (int i = 0; i < BranchChildCount; i++)
            {
                var n = new MPTNode();
                n.Deserialize(reader);
                Children[i] = n;
            }
        }
    }
}
