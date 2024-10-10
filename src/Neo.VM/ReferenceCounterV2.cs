// Copyright (C) 2015-2024 The Neo Project.
//
// ReferenceCounter.cs file belongs to the neo project and is free
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

namespace Neo.VM
{
    /// <summary>
    /// Used for reference counting of objects in the VM.
    /// </summary>
    public sealed class ReferenceCounterV2
    {
        private class Counter
        {
            public int Count;
        }

        private readonly Dictionary<StackItem, Counter> _items = new(ReferenceEqualityComparer.Instance);

        private int _referencesCount = 0;

        /// <summary>
        /// Gets the count of references.
        /// </summary>
        public int Count => _referencesCount;

        /// <summary>
        /// Adds item to Reference Counter
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(StackItem item)
        {
            if (!_items.TryGetValue(item, out var referencesCount))
            {
                _items[item] = new Counter() { Count = 1 };
            }
            else
            {
                referencesCount.Count++;
            }

            // Increment the reference count.
            _referencesCount++;
        }

        /// <summary>
        /// Removes item from Reference Counter
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void Remove(StackItem item)
        {
            if (!_items.TryGetValue(item, out var referencesCount))
            {
                throw new InvalidOperationException("Reference was not added");
            }
            else
            {
                referencesCount.Count--;

                if (referencesCount.Count < 0)
                {
                    throw new InvalidOperationException("Reference was not added");
                }
            }

            // Decrement the reference count.
            _referencesCount--;
        }
    }
}
