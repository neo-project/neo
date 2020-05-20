using Neo.IO;
using Neo.Persistence;
using System;

namespace Neo.Cryptography.MPT
{
    public partial class MPTTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private readonly MPTDb db;
        private MPTNode root;

        public MPTTrie(UInt256 root, ISnapshot store)
        {
            if (store is null)
                throw new ArgumentNullException();

            this.db = new MPTDb(store);

            if (root is null)
            {
                this.root = HashNode.EmptyNode;
            }
            else
            {
                this.root = new HashNode(root);
            }
        }

        public MPTNode Resolve(HashNode n)
        {
            return db.Node(n.Hash);
        }

        public UInt256 GetRoot()
        {
            return root.Hash;
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
