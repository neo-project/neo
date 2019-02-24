using System;
using System.Linq;

namespace Neo.Ledger.MPT
{
    public class MPTRemove
    {
        private readonly Func<byte[], MerklePatriciaNode> _getDb;
        private readonly Func<byte[], bool> _removeDb;
        private readonly Func<byte[], MerklePatriciaNode, MerklePatriciaNode> _setDb;
        private readonly Func<byte[]> _getRoot;
        private readonly Action<byte[]> _setRoot;

        internal MPTRemove(Func<byte[], MerklePatriciaNode> _getDb, Func<byte[], bool> _removeDb,
            Func<byte[], MerklePatriciaNode, MerklePatriciaNode> _setDb, Func<byte[]> _getRoot, Action<byte[]> _setRoot)
        {
            this._getDb = _getDb;
            this._removeDb = _removeDb;
            this._setDb = _setDb;
            this._getRoot = _getRoot;
            this._setRoot = _setRoot;
        }

        public bool Remove(byte[] key)
        {
            var root = _getRoot();
            if (root == null)
            {
                return false;
            }

            var removido = Remove(root, key.ConvertToNibble());
            var resp = removido == null || !root.SequenceEqual(removido);
            if (resp)
            {
                _removeDb(root);
            }

            _setRoot(removido);
            return resp;
        }

        private byte[] Remove(byte[] nodeHash, byte[] path)
        {
            if (nodeHash == null)
            {
                return null;
            }

            var node = _getDb(nodeHash);
            byte[] respHash = null;
            if (node.IsLeaf)
            {
                respHash = RemoveLeaf(nodeHash, path, node);
            }

            if (node.IsExtension)
            {
                respHash = RemoveExtension(nodeHash, path, node);
            }

            if (node.IsBranch)
            {
                respHash = RemoveBranch(nodeHash, path, node);
            }

            return respHash;
        }

        private byte[] RemoveLeaf(byte[] nodeHash, byte[] path, MerklePatriciaNode node)
        {
            if (node.Path.SequenceEqual(path))
            {
                _removeDb(nodeHash);
                return null;
            }

            _setDb(nodeHash, node);
            return nodeHash;
        }

        private byte[] RemoveExtension(byte[] nodeHash, byte[] path, MerklePatriciaNode node)
        {
            if (path.Length >= node.Path.Length &&
                path.Take(node.Path.Length).ToArray().SequenceEqual(node.Path))
            {
                node.Next = Remove(node.Next, path.Skip(node.Path.Length).ToArray());
                var nodeNext = _getDb(node.Next);
                if (nodeNext.IsLeaf || nodeNext.IsExtension)
                {
                    _removeDb(node.Next);
                    nodeNext.Path = node.Path.Concat(nodeNext.Path).ToArray();
                    node = nodeNext;
                }
                else
                {
                    var cont = 0;
                    var index = -1;
                    // TODO FIXME Criar um iterator pra isso
//                    for (var i = 0; i < nodeNext.Length - 2 && cont < 2; i++)
//                    {
//                        if (nodeNext[i] != null)
//                        {
//                            cont++;
//                            index = i;
//                        }
//                    }
                    (index, cont) = nodeNext.IndexAndCountNotNullHashes();

                    if (cont == 1 && nodeNext.Value == null)
                    {
                        _removeDb(node.Next);
                        node.Path = node.Path.Concat(new[] {(byte) index}).ToArray();
                        node.Next = nodeNext[index];

                        nodeNext = _getDb(node.Next);
                        if (nodeNext.IsExtension)
                        {
                            _removeDb(node.Next);
                            node.Path = node.Path.Concat(nodeNext.Path).ToArray();
                            node.Next = nodeNext.Next;
                        }
                    }
                }
            }
            else
            {
                _setDb(nodeHash, node);
                return nodeHash;
            }

            _removeDb(nodeHash);
            nodeHash = node.Hash();
            _setDb(nodeHash, node);
            return nodeHash;
        }

        private byte[] RemoveBranch(byte[] nodeHash, byte[] path, MerklePatriciaNode node)
        {
            if (path.Length == 0)
            {
                node.Key = null;
                node.Value = null;
            }
            else if (node[path[0]] != null)
            {
                node[path[0]] = Remove(node[path[0]], path.Skip(1).ToArray());
            }

            var contar = 0;
            var indexInnerNode = 0;
//            for (var i = 0; i < node.Length - 2; i++)
//            {
//                if (node[i] == null) continue;
//                contar++;
//                indexInnerNode = i;
//            }
            (indexInnerNode, contar) = node.IndexAndCountNotNullHashes();

            if (contar == 0)
            {
                var newNode = MerklePatriciaNode.LeafNode();
                newNode.Path = new byte[0];
                newNode.Key = node.Key;
                newNode.Value = node.Value;
                node = newNode;
            }
            else if (contar == 1 && node.Value == null)
            {
                var innerNodeHash = node[indexInnerNode];
                var innerNode = _getDb(innerNodeHash);
                if (innerNode.IsLeaf)
                {
                    _removeDb(innerNodeHash);
                    node = MerklePatriciaNode.LeafNode();
                    node.Path = new[] {(byte) indexInnerNode}.Concat(innerNode.Path).ToArray();
                    node.Key = innerNode.Key;
                    node.Value = innerNode.Value;
                }
                else if (innerNode.IsExtension)
                {
                    _removeDb(innerNodeHash);
                    node = MerklePatriciaNode.ExtensionNode();
                    node.Path = new[] {(byte) indexInnerNode}.Concat(innerNode.Path).ToArray();
                    node.Next = innerNode.Next;
                }
                else if (innerNode.IsBranch)
                {
                    node = MerklePatriciaNode.ExtensionNode();
                    node.Path = new[] {(byte) indexInnerNode};
                    node.Next = innerNodeHash;
                }
            }

            _removeDb(nodeHash);
            nodeHash = node.Hash();
            _setDb(nodeHash, node);
            return nodeHash;
        }
    }
}