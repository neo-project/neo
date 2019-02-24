using System;
using System.Linq;
using Neo.IO;

namespace Neo.Ledger.MPT
{
    public class MPTGet
    {
        private readonly Func<byte[], MerklePatriciaNode> _getDb;
        private readonly Func<byte[]> _getRoot;

        internal MPTGet(Func<byte[], MerklePatriciaNode> _getDb, Func<byte[]> _getRoot)
        {
            this._getDb = _getDb;
            this._getRoot = _getRoot;
        }

        public byte[] Get(byte[] key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var root = _getRoot();
            return root == null ? null : Get(_getDb(root), key.ConvertToNibble());
        }

        private byte[] Get(MerklePatriciaNode node, byte[] path)
        {
            while (true)
            {
                if (node == null)
                {
                    return null;
                }

                if (node.IsLeaf)
                {
                    return node.Path.SequenceEqual(path) ? node.Value : null;
                }

                if (path.Length == 0 && !node.IsExtension)
                {
                    return node.Value;
                }

                if (node.IsExtension)
                {
                    if (path.SequenceEqual(node.Path))
                    {
                        node = _getDb(node.Next);
                        path = new byte[0];
                        continue;
                    }

                    if (node.Path.Length < path.Length &&
                        path.Take(node.Path.Length).ToArray().SequenceEqual(node.Path))
                    {
                        var node1 = node;
                        node = _getDb(node.Next);
                        path = path.Skip(node1.Path.Length).ToArray();
                        continue;
                    }

                    return null;
                }

                // Branch node
                if (node[path[0]] != null)
                {
                    node = _getDb(node[path[0]]);
                    path = path.Skip(1).ToArray();
                    continue;
                }

                return null;
            }
        }
    }
}