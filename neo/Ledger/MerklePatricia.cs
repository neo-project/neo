using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Neo.IO;

namespace Neo.Ledger
{
    /// <summary>
    /// Modified Merkel Patricia Tree.
    /// Note: It is not a thread safe implementation.
    /// </summary>
    public class MerklePatricia : StateBase, ICloneable<MerklePatricia>, IEquatable<MerklePatricia>
    {
        private byte[] _rootHash;

        private readonly Dictionary<byte[], MerklePatriciaNode> _db = new
            Dictionary<byte[], MerklePatriciaNode>(new ByteArrayComparer());

        /// <summary>
        /// Get and set the key and valeu pairs of the tree.
        /// </summary>
        /// <param name="key">The string key that indicates the reference.</param>
        public string this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException("The key parameter can not be null");
                }
                var resp = this[Encoding.UTF8.GetBytes(key)];
                return resp == null ? null : Encoding.UTF8.GetString(resp);
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException("The key parameter can not be null");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("The value parameter can not be null");
                }
                this[Encoding.UTF8.GetBytes(key)] = Encoding.UTF8.GetBytes(value);
            }
        }

        /// <summary>
        /// Get and set the key and valeu pairs of the tree.
        /// </summary>
        /// <param name="key">The key that indicates the reference.</param>
        public byte[] this[byte[] key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException("The key parameter can not be null");
                }
                return _rootHash == null ? null : Get(_db[_rootHash], ConvertToNibble(key));
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException("The key parameter can not be null");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("The value parameter can not be null");
                }
                var node = _rootHash == null ? null : _db[_rootHash];
                if (_rootHash != null)
                {
                    _db.Remove(_rootHash);
                }

                _rootHash = Set(node, ConvertToNibble(key), key, value);
            }
        }

        private static byte[] ConvertToNibble(byte[] key)
        {
            var resp = new byte[key.Length * 2];
            for (var i = 0; i < key.Length; i++)
            {
                resp[2 * i] = (byte) (key[i] / 16);
                resp[2 * i + 1] = (byte) (key[i] % 16);
            }

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
                if (path.Length == 0 || path.SequenceEqual(node.Path))
                {
                    node.Key = key;
                    node.Value = value;
                }
                else if (node.Path.Length == 0 || path[0] != node.Path[0])
                {
                    var innerHash = Set(MerklePatriciaNode.BranchNode(), node.Path, node.Key, node.Value);
                    node = _db[innerHash];
                    _db.Remove(innerHash);
                    innerHash = Set(node, path, key, value);
                    node = _db[innerHash];
                    _db.Remove(innerHash);
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
                            innerNode.Next = Set(MerklePatriciaNode.BranchNode(), node.Path.Skip(pos + 1).ToArray(),
                                node.Key,
                                node.Value);
                            node = innerNode;
                            innerNode = _db[node.Next];
                            _db.Remove(node.Next);
                            node.Next = Set(innerNode, path.Skip(pos + 1).ToArray(), key, value);
                            break;
                        }
                    }
                }
            }
            else if (node.IsExtension)
            {
                if (path.Length == 0 || path.SequenceEqual(node.Path))
                {
                    var innerHash = node.Next;
                    var innerNode = _db[innerHash];
                    _db.Remove(innerHash);
                    node.Next = Set(innerNode, new byte[0], key, value);
                }
                else if (node.Path.Length == 0 || path[0] != node.Path[0])
                {
                    var newHash = Set(MerklePatriciaNode.BranchNode(), path, key, value);
                    var newNode = _db[newHash];
                    _db.Remove(newHash);
                    newNode[node.Path[1]] = node.Next;
                    node.Path = node.Path.Skip(1).ToArray();
                    node = newNode;
                }
                else
                {
                    for (var pos = 0;; pos++)
                    {
                        if (pos + 1 == node.Path.Length)
                        {
                            var innerHash = node.Next;
                            node.Next = Set(_db[innerHash], path.Skip(pos + 1).ToArray(), key, value);
                            _db.Remove(innerHash);
                            break;
                        }

                        if (pos + 1 == path.Length)
                        {
                            var newHash = Set(MerklePatriciaNode.BranchNode(), new byte[0], key, value);
                            var newNode = _db[newHash];
                            _db.Remove(newHash);
                            newNode[node.Path[pos + 1]] = node.Next;
                            node.Path = node.Path.Skip(pos + 1).ToArray();
                            node = newNode;
                            break;
                        }

                        if (path[pos + 1] != node.Path[pos + 1])
                        {
                            var oldExtension = node;
                            node = MerklePatriciaNode.ExtensionNode();
                            node.Path = oldExtension.Path.Take(pos + 1).ToArray();
                            node.Next = Set(MerklePatriciaNode.BranchNode(), path.Skip(pos + 1).ToArray(), key, value);

                            var nodeNext = _db[node.Next];
                            _db.Remove(node.Next);
                            var oldExtensionPath = oldExtension.Path;
                            oldExtension.Path = oldExtension.Path.Skip(pos + 2).ToArray();
                            nodeNext[oldExtensionPath[pos + 1]] = oldExtension.Hash();
                            _db[nodeNext[oldExtensionPath[pos + 1]]] = oldExtension;

                            node.Next = nodeNext.Hash();
                            _db[node.Next] = nodeNext;

                            break;
                        }
                    }
                }
            }
            else
            {
                if (path.Length == 0)
                {
                    node.Key = key;
                    node.Value = value;
                }
                else
                {
                    var innerHash = node[path[0]];
                    var innerNode = innerHash != null ? _db[innerHash] : null;
                    if (innerHash != null)
                    {
                        _db.Remove(innerHash);
                    }

                    node[path[0]] = Set(innerNode, path.Skip(1).ToArray(), key, value);
                }
            }

            var hashNode = node.Hash();
            _db[hashNode] = node;
            return hashNode;
        }

        private byte[] Get(MerklePatriciaNode node, byte[] path)
        {
            if (node == null)
            {
                return null;
            }

            if (node.IsLeaf)
            {
                if (path.Length == 0)
                {
                    return node.Value;
                }

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
                    return Get(_db[node.Next], new byte[0]);
                }

                if (node.Path.Length < path.Length && path.Take(node.Path.Length).ToArray().SequenceEqual(node.Path))
                {
                    return Get(_db[node.Next], path.Skip(node.Path.Length).ToArray());
                }

                return null;
            }

            // Branch node
            return node[path[0]] != null ? Get(_db[node[path[0]]], path.Skip(1).ToArray()) : null;
        }

        /// <summary>
        /// Test if contains a specific key.
        /// </summary>
        /// <param name="key">Key to be tested.</param>
        /// <returns>true in the case the tree contains the key.</returns>
        public bool ContainsKey(byte[] key) => this[key] != null;

        public bool ContainsKey(string key) => ContainsKey(Encoding.UTF8.GetBytes(key));

        /// <summary>
        /// Test if the tree contains a specific value.
        /// Takes O(n) operations.
        /// </summary>
        /// <param name="value">Value to look for.</param>
        /// <returns>true if the value is present.</returns>
        public bool ContainsValue(byte[] value) =>
            _db.Any(x => x.Value.Value != null && x.Value.Value.SequenceEqual(value));

        public bool ContainsValue(string value) => ContainsValue(Encoding.UTF8.GetBytes(value));

        /// <summary>
        /// Removes a value for a specific key.
        /// </summary>
        /// <param name="key">Remove this key from the tree.</param>
        /// <returns>true is the key was present and sucessifully removed.</returns>
        public bool Remove(string key) => Remove(Encoding.UTF8.GetBytes(key));

        /// <summary>
        /// Removes a value for a specific key.
        /// </summary>
        /// <param name="key">Remove this key from the tree.</param>
        /// <returns>true is the key was present and sucessifully removed.</returns>
        public bool Remove(byte[] key)
        {
            if (_rootHash == null)
            {
                return false;
            }

            var removido = Remove(_rootHash, ConvertToNibble(key));
            var resp = removido == null || !_rootHash.SequenceEqual(removido);
            if (resp)
            {
                _db.Remove(_rootHash);
            }

            _rootHash = removido;
            return resp;
        }

        private byte[] Remove(byte[] nodeHash, byte[] path)
        {
            if (nodeHash == null)
            {
                return null;
            }

            var node = _db[nodeHash];
            if (node.IsLeaf)
            {
                if (node.Path.SequenceEqual(path))
                {
                    _db.Remove(nodeHash);
                    return null;
                }

                _db[nodeHash] = node;
                return nodeHash;
            }

            if (node.IsExtension)
            {
                if (path.Length >= node.Path.Length &&
                    path.Take(node.Path.Length).ToArray().SequenceEqual(node.Path))
                {
                    node.Next = Remove(node.Next, path.Skip(node.Path.Length).ToArray());
                    var nodeNext = _db[node.Next];
                    if (nodeNext.IsLeaf || nodeNext.IsExtension)
                    {
                        _db.Remove(node.Next);
                        nodeNext.Path = node.Path.Concat(nodeNext.Path).ToArray();
                        node = nodeNext;
                    }
                }
                else
                {
                    _db[nodeHash] = node;
                    return nodeHash;
                }
            }
            else if (node.IsBranch)
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
                for (var i = 0; i < node.Length - 2; i++)
                {
                    if (node[i] == null) continue;
                    contar++;
                    indexInnerNode = i;
                }

                if (contar == 0)
                {
                    var newNode = MerklePatriciaNode.LeafNode();
                    newNode.Path = new byte[0];
                    newNode.Key = node.Key;
                    newNode.Value = node.Value;
                    node = newNode;
                }
                else if (contar == 1)
                {
                    if (node[node.Length - 1] == null)
                    {
                        var innerNodeHash = node[indexInnerNode];
                        var innerNode = _db[innerNodeHash];
                        if (innerNode.IsLeaf)
                        {
                            _db.Remove(innerNodeHash);
                            node = MerklePatriciaNode.LeafNode();
                            node.Path = new[] {(byte) indexInnerNode}.Concat(innerNode.Path).ToArray();
                            node.Key = innerNode.Key;
                            node.Value = innerNode.Value;
                        }
                    }
                }
            }

            _db.Remove(nodeHash);
            nodeHash = node.Hash();
            _db[nodeHash] = node;
            return nodeHash;
        }

        /// <summary>
        /// Checks if the hashes correspond to their nodes.
        /// </summary>
        /// <returns>In the case the validation is Ok.</returns>
        public bool Validate() => _rootHash == null || Validate(_rootHash, _db[_rootHash]);

        private bool Validate(byte[] nodeHash, MerklePatriciaNode node)
        {
            while (true)
            {
                if (!nodeHash.SequenceEqual(node.Hash()))
                {
                    return false;
                }

                if (node.IsExtension)
                {
                    nodeHash = node.Next;
                    node = _db[node.Next];
                    continue;
                }

                if (node.IsLeaf)
                {
                    return true;
                }

                for (var i = 0; i < node.Length - 2; i++)
                {
                    var subNodeHash = node[i];
                    if (subNodeHash != null && !Validate(subNodeHash, _db[subNodeHash]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <inheritdoc />
        public override string ToString() => _rootHash == null ? "{}" : ToString(_db[_rootHash]);

        private string ToString(MerklePatriciaNode node)
        {
            if (node.IsExtension)
            {
                return $"{{\"{node.Path.ByteToHexString(false, false)}\": {ToString(_db[node.Next])}}}";
            }

            if (node.IsLeaf)
            {
                return node.ToString();
            }

            var resp = new StringBuilder("{");
            var virgula = false;
            for (var i = 0; i < node.Length; i++)
            {
                if (node[i] == null) continue;
                resp.Append(virgula ? "," : "")
                    .Append(i < node.Length - 2
                        ? $"\"{i:x}\":{ToString(_db[node[i]])}"
                        : $"\"{i:x}\":\"{node[i].ByteToHexString(false, false)}\"");

                virgula = true;
            }

            return resp.Append("}").ToString();
        }

        /// <inheritdoc />
        public MerklePatricia Clone()
        {
            var resp = new MerklePatricia();
            foreach (var entry in _db)
            {
                resp._db[entry.Key.ToArray()] = entry.Value.Clone();
            }

            return resp;
        }

        /// <inheritdoc />
        public void FromReplica(MerklePatricia replica)
        {
            _db.Clear();
            foreach (var entry in replica._db)
            {
                _db[entry.Key.ToArray()] = entry.Value.Clone();
            }
        }

        /// <inheritdoc />
        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            _db.Clear();
            _rootHash = reader.ReadVarBytes();
            var size = reader.ReadVarInt();
            for (var i = 0ul; i < size; i++)
            {
                var key = reader.ReadVarBytes();
                var value = MerklePatriciaNode.ExtensionNode();
                value.Deserialize(reader);
                _db[key] = value;
            }
        }

        public int Count() => _db.Count(x => x.Value.IsLeaf || (x.Value.IsBranch && x.Value.Value != null));

        /// <inheritdoc />
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(_rootHash);
            writer.WriteVarInt(_db.Count);
            foreach (var it in _db)
            {
                writer.WriteVarBytes(it.Key);
                writer.Write(it.Value);
            }
        }

        /// <inheritdoc />
        public bool Equals(MerklePatricia other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!_rootHash.SequenceEqual(other._rootHash) || _db.Count != other._db.Count)
            {
                return false;
            }

            foreach (var it in _db)
            {
                var otherV = other._db[it.Key];
                if (!otherV.Equals(it.Value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((MerklePatricia) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => _rootHash != null ? _rootHash.GetHashCode() : 0;
    }
}