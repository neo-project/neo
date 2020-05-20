using Neo.IO;
using Neo.Persistence;
using System;

namespace Neo.Cryptography.MPT
{
    public partial class MPTTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private const byte Prefix = 0xf0;

        private readonly ISnapshot store;
        private MPTNode root;

        public MPTNode Root => root;

        public MPTTrie(ISnapshot store, UInt256 root)
        {
            this.store = store ?? throw new ArgumentNullException();
            this.root = root is null ? HashNode.EmptyNode : new HashNode(root);
        }

        private MPTNode Resolve(HashNode n)
        {
            var data = store.TryGet(Prefix, n.Hash.ToArray());
            return MPTNode.Decode(data);
        }

        private static byte[] ToNibbles(ReadOnlySpan<byte> path)
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
