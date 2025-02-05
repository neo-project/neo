// Copyright (C) 2015-2025 The Neo Project.
//
// Tree.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.Plugins.RpcServer
{
    class Tree<T>
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
