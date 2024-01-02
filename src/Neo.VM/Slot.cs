// Copyright (C) 2015-2024 The Neo Project.
//
// Slot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Collections;
using System.Collections.Generic;

namespace Neo.VM
{
    /// <summary>
    /// Used to store local variables, arguments and static fields in the VM.
    /// </summary>
    public class Slot : IReadOnlyList<StackItem>
    {
        private readonly ReferenceCounter referenceCounter;
        private readonly StackItem[] items;

        /// <summary>
        /// Gets the item at the specified index in the slot.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get.</param>
        /// <returns>The item at the specified index in the slot.</returns>
        public StackItem this[int index]
        {
            get
            {
                return items[index];
            }
            internal set
            {
                ref var oldValue = ref items[index];
                referenceCounter.RemoveStackReference(oldValue);
                oldValue = value;
                referenceCounter.AddStackReference(value);
            }
        }

        /// <summary>
        /// Gets the number of items in the slot.
        /// </summary>
        public int Count => items.Length;

        /// <summary>
        /// Creates a slot containing the specified items.
        /// </summary>
        /// <param name="items">The items to be contained.</param>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        public Slot(StackItem[] items, ReferenceCounter referenceCounter)
        {
            this.referenceCounter = referenceCounter;
            this.items = items;
            foreach (StackItem item in items)
                referenceCounter.AddStackReference(item);
        }

        /// <summary>
        /// Create a slot of the specified size.
        /// </summary>
        /// <param name="count">Indicates the number of items contained in the slot.</param>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        public Slot(int count, ReferenceCounter referenceCounter)
        {
            this.referenceCounter = referenceCounter;
            this.items = new StackItem[count];
            System.Array.Fill(items, StackItem.Null);
            referenceCounter.AddStackReference(StackItem.Null, count);
        }

        internal void ClearReferences()
        {
            foreach (StackItem item in items)
                referenceCounter.RemoveStackReference(item);
        }

        IEnumerator<StackItem> IEnumerable<StackItem>.GetEnumerator()
        {
            foreach (StackItem item in items) yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }
}
