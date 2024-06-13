// Copyright (C) 2015-2024 The Neo Project.
//
// CompoundTypeReferenceCountChecker.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.VM;

internal class CompoundTypeReferenceCountChecker(int maxItems = 2048)
{
    public void CheckCompoundType(CompoundType rootItem)
    {
        if (rootItem is null) throw new ArgumentNullException(nameof(rootItem));

        var visited = new HashSet<StackItem>(ReferenceEqualityComparer.Instance);
        var itemCount = TraverseCompoundType(rootItem, visited, 0);

        if (itemCount > maxItems)
        {
            throw new InvalidOperationException($"Exceeded maximum of {maxItems} items.");
        }
    }

    private int TraverseCompoundType(CompoundType rootItem, HashSet<StackItem> visited, int itemCount)
    {
        var stack = new Stack<CompoundType>();
        stack.Push(rootItem);
        visited.Add(rootItem);
        itemCount++;

        while (stack.Count > 0)
        {
            var currentCompound = stack.Pop();

            foreach (var subItem in currentCompound.SubItems)
            {
                // if a compound type item has reference counter assigned
                // Then its subitem is referred.
                if (subItem is CompoundType compoundType)
                {
                    // If a compound type has no reference counter
                    // Then this compound type is problematic
                    if (compoundType.ReferenceCounter == null)
                    {
                        throw new InvalidOperationException("Invalid stackitem being pushed.");
                    }

                    // Check if this subItem has been visited already
                    if (!visited.Add(compoundType))
                    {
                        continue;
                    }

                    // Add the subItem to the stack and increment the itemCount
                    stack.Push(compoundType);
                    itemCount++;

                    // Check if the itemCount exceeds the maximum allowed items
                    if (itemCount > maxItems)
                    {
                        throw new InvalidOperationException($"Exceeded maximum of {maxItems} items.");
                    }

                }
            }
        }

        return itemCount;
    }
}
