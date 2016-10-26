using System.Collections.Generic;

namespace AntShares.IO.Caching
{
    internal class TreeNode<TValue>
    {
        private List<TreeNode<TValue>> _children = new List<TreeNode<TValue>>();

        public TValue Item { get; set; }
        public uint Height { get; internal set; }
        public TreeNode<TValue> Parent { get; internal set; }
        public IReadOnlyCollection<TreeNode<TValue>> Children => _children;

        public TreeNode(TValue item, TreeNode<TValue> parent)
        {
            this.Item = item;
            this.Height = parent?.Height + 1 ?? 0;
            this.Parent = parent;
            if (parent != null)
                parent._children.Add(this);
        }
    }
}
