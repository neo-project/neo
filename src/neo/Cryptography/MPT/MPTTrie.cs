using Neo.IO;
using Neo.IO.Caching;
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

        public MPTNode Root => root;

        public MPTTrie(ISnapshot store, UInt256 root, bool full_state = false)
        {
            this.store = store ?? throw new ArgumentNullException();
            this.root = root is null ? MPTNode.EmptyNode : new HashNode(root);
            this.full = full_state;
        }

        private MPTNode Resolve(UInt256 hash)
        {
            var data = store.TryGet(Prefix, hash.ToArray());
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

        private void PutNode(MPTNode np)
        {
            var n = Resolve(np.Hash);
            if (n is null)
            {
                np.Reference = 1;
            }
            else
            {
                n.Reference++;
            }
            store.Put(Prefix, np.Hash.ToArray(), np.EncodeWithReference());
        }

        private void DeleteNode(UInt256 hash)
        {
            var n = Resolve(hash);
            if (n is null) return;
            if (1 < n.Reference)
            {
                n.Reference--;
                store.Put(Prefix, hash.ToArray(), n.EncodeWithReference());
                return;
            }
            store.Delete(Prefix, hash.ToArray());
        }
    }
}
