// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public class TreeNode<T>
    {
        public T Item { get; }
        public TreeNode<T> Parent { get; }

        private readonly List<TreeNode<T>> children = new();

        internal TreeNode(T item, TreeNode<T> parent)
        {
            Item = item;
            Parent = parent;
        }

        public TreeNode<T> AddChild(T item)
        {
            TreeNode<T> child = new(item, this);
            children.Add(child);
            return child;
        }

        internal IEnumerable<T> GetItems()
        {
            yield return Item;
            foreach (var child in children)
                foreach (T item in child.GetItems())
                    yield return item;
        }
    }
}
