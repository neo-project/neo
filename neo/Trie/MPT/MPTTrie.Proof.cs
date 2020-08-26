using Neo.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Trie.MPT
{
    public partial class MPTTrie
    {
        public bool GetProof(byte[] key, out HashSet<byte[]> proof)
        {
            proof = new HashSet<byte[]>(ByteArrayEqualityComparer.Default);
            var path = ToNibbles(key);
            return GetProof(ref root, path, proof);
        }

        private bool GetProof(ref MPTNode node, ReadOnlySpan<byte> path, HashSet<byte[]> proof)
        {
            switch (node)
            {
                case LeafNode leafNode:
                    {
                        if (path.IsEmpty)
                        {
                            proof.Add(leafNode.ToArray());//Serialize without References
                            return true;
                        }
                        break;
                    }
                case HashNode hashNode:
                    {
                        if (hashNode.IsEmptyNode) break;
                        var new_node = Resolve(hashNode);
                        if (new_node is null) break;
                        node = new_node;
                        return GetProof(ref node, path, proof);
                    }
                case BranchNode branchNode:
                    {
                        proof.Add(branchNode.ToArray());
                        if (path.Length == 0)
                            return GetProof(ref branchNode.Children[BranchNode.ChildCount - 1], path, proof);
                        return GetProof(ref branchNode.Children[path[0]], path.Slice(1), proof);
                    }
                case ExtensionNode extensionNode:
                    {
                        if (path.StartsWith(extensionNode.Key))
                        {
                            proof.Add(extensionNode.ToArray());
                            return GetProof(ref extensionNode.Next, path.Slice(extensionNode.Key.Length), proof);
                        }
                        break;
                    }
            }
            return false;
        }

        public static bool VerifyProof(UInt256 root, byte[] path, HashSet<byte[]> proof, out byte[] value)
        {
            var store = new MPTProofStore(proof);
            var trie = new MPTTrie(root, store);
            bool result;
            try
            {
                result = trie.TryGet(path, out value);
            }
            catch (Exception)
            {
                value = Array.Empty<byte>();
                result = false;
            }
            return result;
        }
    }
}
