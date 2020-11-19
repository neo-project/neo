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
        private readonly bool full;
        private readonly ISnapshot store;
        private MPTNode root;
        private readonly MPTCache cache;
        public MPTNode Root => root;

        public MPTTrie(ISnapshot store, UInt256 root, bool full_state = false)
        {
            this.store = store ?? throw new ArgumentNullException();
            this.cache = new MPTCache(store, Prefix);
            this.root = root is null ? new MPTNode() : MPTNode.NewHash(root);
            this.full = full_state;
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

        private static byte[] FromNibbles(ReadOnlySpan<byte> path)
        {
            if (path.Length % 2 != 0) throw new FormatException($"MPTTrie.FromNibbles invalid path.");
            var key = new byte[path.Length / 2];
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)(path[i * 2] << 4);
                key[i] |= path[i * 2 + 1];
            }
            return key;
        }

        public void Commit()
        {
            cache.Commit();
        }
    }
}
