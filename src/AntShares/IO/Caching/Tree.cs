using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.IO.Caching
{
    internal class Tree<TKey, TValue>
    {
        private Dictionary<TKey, TreeNode<TValue>> _nodes = new Dictionary<TKey, TreeNode<TValue>>();
        private Dictionary<TKey, TreeNode<TValue>> _leaves = new Dictionary<TKey, TreeNode<TValue>>();

        public TValue this[TKey key]
        {
            get
            {
                return _nodes[key].Item;
            }
            set
            {
                _nodes[key].Item = value;
            }
        }
        public IEnumerable<TreeNode<TValue>> Leaves => _leaves.Values;
        public IReadOnlyDictionary<TKey, TreeNode<TValue>> Nodes => _nodes;
        public TreeNode<TValue> Root { get; private set; }

        public Tree(TKey key, TValue root)
        {
            this.Root = new TreeNode<TValue>(root, null);
            _nodes.Add(key, Root);
            _leaves.Add(key, Root);
        }

        public TreeNode<TValue> Add(TKey key, TValue item, TKey parent)
        {
            if (_nodes.ContainsKey(key)) throw new ArgumentException();
            TreeNode<TValue> node_parent = _nodes[parent];
            TreeNode<TValue> node = new TreeNode<TValue>(item, node_parent);
            _nodes.Add(key, node);
            _leaves.Add(key, node);
            if (_leaves.ContainsKey(parent)) _leaves.Remove(parent);
            return node;
        }

        public TreeNode<TValue> FindCommonNode(params TreeNode<TValue>[] nodes)
        {
            if (nodes.Length == 0) throw new ArgumentException();
            TreeNode<TValue>[] nodes2 = new TreeNode<TValue>[nodes.Length];
            Array.Copy(nodes, nodes2, nodes.Length);
            uint height = nodes2.Min(p => p.Height);
            for (int i = 0; i < nodes2.Length; i++)
            {
                while (nodes2[i].Height > height)
                {
                    nodes2[i] = nodes2[i].Parent;
                }
            }
            for (nodes2 = nodes2.Distinct().ToArray(); nodes2.Length > 1; nodes2 = nodes2.Distinct().ToArray())
            {
                for (int i = 0; i < nodes2.Length; i++)
                {
                    nodes2[i] = nodes2[i].Parent;
                }
            }
            return nodes2[0];
        }
    }
}
