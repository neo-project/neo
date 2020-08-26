using Neo.IO.Json;
using System;

namespace Neo.Trie.MPT
{
    public partial class MPTTrie
    {
        public static byte Version = 0;
        private MPTNode root;
        private readonly MPTDb db;

        public MPTTrie(UInt256 root, IKVStore store)
        {
            if (store is null)
                throw new System.ArgumentNullException();

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

        public UInt256 GetRoot()
        {
            return root.GetHash();
        }

        public ReadOnlySpan<byte> ToNibbles(ReadOnlySpan<byte> path)
        {
            if (path == null || path.IsEmpty) return default;
            var key = new byte[path.Length * 2];
            for (int i = 0; i < path.Length; i++)
            {
                key[i * 2] = (byte)(path[i] >> 4);
                key[i * 2 + 1] = (byte)(path[i] & 0x0F);
            }
            return key;
        }

        public byte[] FromNibbles(ReadOnlySpan<byte> path)
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

        public MPTNode Resolve(HashNode hn)
        {
            return db.Node(hn.Hash);
        }

        public JObject ToJson()
        {
            return root.ToJson();
        }
    }
}
