// Copyright (C) 2015-2024 The Neo Project.
//
// ReferenceCounterV2.cs file belongs to the neo project and is free
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
    public sealed class ReferenceCounterV2 : IReferenceCounter
    {
        private class Counter
        {
            public int Count;
        }

        private readonly Dictionary<StackItem, Counter> _items = new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Gets the count of references.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Adds item to Reference Counter
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="count">Number of similar entries</param>
        public void AddStackReference(StackItem item, int count = 1)
        {
            if (!_items.TryGetValue(item, out var referencesCount))
            {
                _items[item] = new Counter() { Count = count };
            }
            else
            {
                referencesCount.Count += count;
            }

            // Increment the reference count.
            Count += count;
        }

        /// <summary>
        /// Removes item from Reference Counter
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveStackReference(StackItem item)
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
            Count--;
        }

        public void AddReference(StackItem item, CompoundType parent)
        {
            AddStackReference(item);
        }

        public void RemoveReference(StackItem item, CompoundType parent)
        {
            RemoveStackReference(item);
        }

        public void AddZeroReferred(StackItem item)
        {
            // This version don't use this method
            // AddStackReference(item);
        }

        /// <summary>
        /// Checks and processes items that have zero references.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int CheckZeroReferred()
        {
            return Count;
        }
    }
}
