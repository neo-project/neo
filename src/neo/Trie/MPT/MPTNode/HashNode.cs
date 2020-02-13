
namespace Neo.Trie.MPT
{
    public class HashNode : MPTNode
    {
        public byte[] Hash;

        public override int Size => 1 + Hash.Length;
        public HashNode()
        {
            nType = NodeType.HashNode;
        }

        public HashNode(byte[] hash)
        {
            nType = NodeType.HashNode;
            Hash = (byte[])hash.Clone();
        }

        protected override byte[] CalHash()
        {
            return (byte[])Hash.Clone();
        }

        public static HashNode EmptyNode()
        {
            return new HashNode(new byte[] { });
        }

        public bool IsEmptyNode => Hash.Length == 0;
    }
}
