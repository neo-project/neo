using Neo.IO;
using Neo.Persistence;
using System;

namespace Neo.Cryptography.MPT
{
    public partial class MPTReadOnlyTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private MPTReadOnlyDb rodb;
        protected MPTNode root;

        public MPTReadOnlyTrie(UInt256 root, IReadOnlyStore store, byte prefix)
        {
            if (store is null)
                throw new System.ArgumentNullException();

            this.rodb = new MPTReadOnlyDb(store, prefix);

            if (root is null)
            {
                this.root = HashNode.EmptyNode();
            }
            else
            {
                this.root = new HashNode(root);
            }
        }

        public MPTNode Resolve(HashNode n)
        {
            return rodb.Node(n.Hash);
        }

        public UInt256 GetRoot()
        {
            return root.GetHash();
        }

        protected static byte[] ToNibbles(ReadOnlySpan<byte> path)
        {
            var result = new byte[path.Length * 2];
            for (int i = 0; i < path.Length; i++)
            {
                result[i * 2] = (byte)(path[i] >> 4);
                result[i * 2 + 1] = (byte)(path[i] & 0x0F);
            }
            return result;
        }
    }
}
