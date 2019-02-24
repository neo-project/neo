using System;
using System.Linq;

namespace Neo.Ledger.MPT.Operation
{
    /// <summary>
    /// MPT set operation.
    /// </summary>
    internal class MPTSet
    {
        /// <summary>
        /// Delegate to get data from the database.
        /// </summary>
        private readonly Func<byte[], MerklePatriciaNode> _getDb;

        /// <summary>
        /// Delegate to remove data from the database.
        /// </summary>
        private readonly Func<byte[], bool> _removeDb;

        /// <summary>
        /// Delegate to set data on the database.
        /// </summary>
        private readonly Func<byte[], MerklePatriciaNode, MerklePatriciaNode> _setDb;

        /// <summary>
        /// Delegate to get the root hash.
        /// </summary>
        private readonly Func<byte[]> _getRoot;

        /// <summary>
        /// Delegate to change the root hash.
        /// </summary>
        private readonly Action<byte[]> _setRoot;

        /// <summary>
        /// MPT set operation.
        /// </summary>
        /// <param name="_getDb">Delegate to get data from the database.</param>
        /// <param name="_removeDb">Delegate to remove data from the database.</param>
        /// <param name="_setDb">Delegate to set data on the database.</param>
        /// <param name="_getRoot">Delegate to get the root hash.</param>
        /// <param name="_setRoot">Delegate to change the root hash.</param>
        internal MPTSet(Func<byte[], MerklePatriciaNode> _getDb, Func<byte[], bool> _removeDb,
            Func<byte[], MerklePatriciaNode, MerklePatriciaNode> _setDb, Func<byte[]> _getRoot, Action<byte[]> _setRoot)
        {
            this._getDb = _getDb;
            this._removeDb = _removeDb;
            this._setDb = _setDb;
            this._getRoot = _getRoot;
            this._setRoot = _setRoot;
        }

        /// <summary>
        /// Set a key-value pair in the MPT.
        /// </summary>
        /// <param name="key">The key to be inserted.</param>
        /// <param name="value">The value to be inserted.</param>
        /// <returns>The last value.</returns>
        /// <exception cref="ArgumentNullException">In the case that a null key is used.</exception>
        public byte[] Set(byte[] key, byte[] value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var root = _getRoot();
            var node = root == null ? null : _getDb(root);
            if (root != null)
            {
                _removeDb(root);
            }

            var resp = Set(node, key.ConvertToNibble(), key, value);
            _setRoot(resp);
            return resp;
        }

        private byte[] Set(MerklePatriciaNode node, byte[] path, byte[] key, byte[] value)
        {
            if (node == null)
            {
                node = MerklePatriciaNode.LeafNode();
                node.Path = path;
                node.Key = key;
                node.Value = value;
            }
            else if (node.IsLeaf)
            {
                return SetLeaf(node, path, key, value);
            }
            else if (node.IsExtension)
            {
                return SetExtension(node, path, key, value);
            }
            else
            {
                return SetBranch(node, path, key, value);
            }

            var hashNode = node.Hash();
            _setDb(hashNode, node);
            return hashNode;
        }

        private byte[] SetLeaf(MerklePatriciaNode node, byte[] path, byte[] key, byte[] value)
        {
            if (path.Length == 0 || path.SequenceEqual(node.Path))
            {
                node.Key = key;
                node.Value = value;
            }
            else if (node.Path.Length == 0 || path[0] != node.Path[0])
            {
                var innerHash = SetBranch(MerklePatriciaNode.BranchNode(), node.Path, node.Key, node.Value);
                node = _getDb(innerHash);
                _removeDb(innerHash);
                innerHash = Set(node, path, key, value);
                node = _getDb(innerHash);
                _removeDb(innerHash);
            }
            else
            {
                for (var pos = 0;; pos++)
                {
                    if (pos + 1 == path.Length || pos + 1 == node.Path.Length ||
                        path[pos + 1] != node.Path[pos + 1])
                    {
                        var innerNode = MerklePatriciaNode.ExtensionNode();
                        innerNode.Path = path.Take(pos + 1).ToArray();
                        innerNode.Next = SetBranch(MerklePatriciaNode.BranchNode(), node.Path.Skip(pos + 1).ToArray(),
                            node.Key,
                            node.Value);
                        node = innerNode;
                        innerNode = _getDb(node.Next);
                        _removeDb(node.Next);
                        node.Next = Set(innerNode, path.Skip(pos + 1).ToArray(), key, value);
                        break;
                    }
                }
            }

            var hashNode = node.Hash();
            _setDb(hashNode, node);
            return hashNode;
        }

        private byte[] SetExtension(MerklePatriciaNode node, byte[] path, byte[] key, byte[] value)
        {
            void ProcessOldExtension(MerklePatriciaNode nodeValue, MerklePatriciaNode oldExtension)
            {
                if (oldExtension.Path.Length == 1)
                {
                    nodeValue[oldExtension.Path[0]] = oldExtension.Next;
                }
                else
                {
                    var position = oldExtension.Path[0];
                    oldExtension.Path = oldExtension.Path.Skip(1).ToArray();
                    nodeValue[position] = oldExtension.Hash();
                    _setDb(nodeValue[position], oldExtension);
                }
            }

            if (path.Length == 0)
            {
                var oldExtension = node;
                _removeDb(node.Hash());
                node = MerklePatriciaNode.BranchNode();
                ProcessOldExtension(node, oldExtension);

                SetBranch(node, path, key, value);
            }
            else if (path.SequenceEqual(node.Path))
            {
                var innerHash = node.Next;
                var innerNode = _getDb(innerHash);
                _removeDb(innerHash);
                node.Next = Set(innerNode, new byte[0], key, value);
            }
            else if (node.Path.Length == 0 || path[0] != node.Path[0])
            {
                var oldExtension = node;
                _removeDb(node.Hash());
                node = MerklePatriciaNode.BranchNode();
                ProcessOldExtension(node, oldExtension);

                SetBranch(node, path, key, value);
            }
            else
            {
                for (var pos = 0;; pos++)
                {
                    if (pos + 1 == node.Path.Length)
                    {
                        var innerHash = node.Next;
                        _removeDb(node.Hash());
                        node.Next = Set(_getDb(innerHash), path.Skip(pos + 1).ToArray(), key, value);
                        _removeDb(innerHash);
                        break;
                    }

                    if (pos + 1 == path.Length)
                    {
                        var oldExtension = node;
                        _removeDb(node.Hash());
                        node = MerklePatriciaNode.ExtensionNode();
                        node.Path = oldExtension.Path.Take(pos + 1).ToArray();

                        var branchNode = MerklePatriciaNode.BranchNode();
                        oldExtension.Path = oldExtension.Path.Skip(pos + 1).ToArray();
                        if (oldExtension.Path.Length == 1)
                        {
                            branchNode[oldExtension.Path[0]] = oldExtension.Next;
                        }
                        else
                        {
                            var position = oldExtension.Path[0];
                            oldExtension.Path = oldExtension.Path.Skip(1).ToArray();
                            branchNode[position] = oldExtension.Hash();
                            _setDb(branchNode[position], oldExtension);
                        }

                        node.Next = SetBranch(branchNode, new byte[0], key, value);
                        break;
                    }

                    if (path[pos + 1] != node.Path[pos + 1])
                    {
                        var oldExtension = node;
                        node = MerklePatriciaNode.ExtensionNode();
                        node.Path = oldExtension.Path.Take(pos + 1).ToArray();
                        node.Next = SetBranch(MerklePatriciaNode.BranchNode(), path.Skip(pos + 1).ToArray(), key,
                            value);

                        var nodeNext = _getDb(node.Next);
                        _removeDb(node.Next);
                        var oldExtensionPath = oldExtension.Path;
                        oldExtension.Path = oldExtension.Path.Skip(pos + 2).ToArray();
                        if (oldExtension.Path.Length > 0)
                        {
                            nodeNext[oldExtensionPath[pos + 1]] = oldExtension.Hash();
                            _setDb(nodeNext[oldExtensionPath[pos + 1]], oldExtension);
                        }
                        else
                        {
                            nodeNext[oldExtensionPath[pos + 1]] = oldExtension.Next;
                        }

                        node.Next = nodeNext.Hash();
                        _setDb(node.Next, nodeNext);

                        break;
                    }
                }
            }

            var hashNode = node.Hash();
            _setDb(hashNode, node);
            return hashNode;
        }

        private byte[] SetBranch(MerklePatriciaNode node, byte[] path, byte[] key, byte[] value)
        {
            if (path.Length == 0)
            {
                node.Key = key;
                node.Value = value;
            }
            else
            {
                var innerHash = node[path[0]];
                var innerNode = innerHash != null ? _getDb(innerHash) : null;
                if (innerHash != null)
                {
                    _removeDb(innerHash);
                }

                node[path[0]] = Set(innerNode, path.Skip(1).ToArray(), key, value);
            }

            var hashNode = node.Hash();
            _setDb(hashNode, node);
            return hashNode;
        }
    }
}