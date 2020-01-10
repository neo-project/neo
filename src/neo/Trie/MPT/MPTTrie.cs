
using System;

namespace Neo.Trie.MPT
{
    public class MPTTrie: Trie
    {
        private MPTDatabase db;
        private MPTNode root;
        public MPTTrie(MPTDatabase db, byte[] root)
        {
            
        }
        
        public MPTTrie(MPTDatabase db, MPTNode root)
        {
            this.db = db;
            this.root = root;
        }

        public byte[] GetRoot => Hash();
        
        public MPTNode Resolve(byte[] hash)
        {
            return db.Node(hash);
        }

        public byte[] Hash()
        {
            return new byte[]{};
        }
        
        public bool TryGet(byte[] path, out byte[] value) 
        {
            return tryGet(ref root, path, out value);
        }

        private bool tryGet(ref MPTNode node, byte[] path, out byte[] value)
        {
            switch(node)
            {
                case ValueNode valueNode:
                {
                    value = (byte[])valueNode.Value.Clone();
                    return true;
                }
                case HashNode hashNode:
                {
                    var result = false;
                    node = Resolve(hashNode.Hash);
                    result = tryGet(ref node, path, out value);
                    return result;
                }
                case BranchNode branchNode:
                {
                    if (0 == path.Length) {
                        return tryGet(ref branchNode.Children[16], path, out value);
                    }
                    return tryGet(ref branchNode.Children[path[0]], path.Skip(1), out value);
                }
                case ExtensionNode extensionNode:
                {
                    var prefix = extensionNode.Key.CommonPrefix(path);
                    if (prefix.Length == extensionNode.Key.Length)
                    {
                        return tryGet(ref extensionNode.Next, path.Skip(prefix.Length), out value);
                    }
                    break;
                }
                case LeafNode leafNode:
                {
                    if (leafNode.Key.Equal(path))
                    {
                        return tryGet(ref leafNode.Value, path, out value);
                    }
                    break;
                }
            }
            value = new byte[]{};
            return false;
        }

        public bool TryUpdate(byte[] path, byte[] value)
        {
            return true;
        }

        private bool tryUpdate(byte[] path, byte[] value)
        {
            return true;
        }

        public bool TryDelete(byte[] path)
        {
            return true;
        }

        private bool tryDelete(byte[] path, out MPTNode newNode)
        {
            newNode = null;
            return true;
        }
    }
}