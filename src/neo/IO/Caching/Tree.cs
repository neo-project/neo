// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public class Tree<T>
    {
        public TreeNode<T> Root { get; private set; }

        public TreeNode<T> AddRoot(T item)
        {
            if (Root is not null)
                throw new InvalidOperationException();
            Root = new TreeNode<T>(item, null);
            return Root;
        }

        public IEnumerable<T> GetItems()
        {
            if (Root is null) yield break;
            foreach (T item in Root.GetItems())
                yield return item;
        }
    }
}
