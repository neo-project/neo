// Copyright (C) 2015-2025 The Neo Project.
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
using Array = System.Array;

namespace Neo.VM
{
    /// <summary>
    /// Used to store local variables, arguments and static fields in the VM.
    /// </summary>
    public class Slot : IReadOnlyList<StackItem>
    {
        private readonly IReferenceCounter _referenceCounter;
        private readonly StackItem[] _items;

        /// <summary>
        /// Gets the item at the specified index in the slot.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get.</param>
        /// <returns>The item at the specified index in the slot.</returns>
        public StackItem this[int index]
        {
            get
            {
                return _items[index];
            }
            internal set
            {
                ref var oldValue = ref _items[index];
                _referenceCounter.RemoveStackReference(oldValue);
                oldValue = value;
                _referenceCounter.AddStackReference(value);
            }
        }

        /// <summary>
        /// Gets the number of items in the slot.
        /// </summary>
        public int Count => _items.Length;

        /// <summary>
        /// Creates a slot containing the specified items.
        /// </summary>
        /// <param name="items">The items to be contained.</param>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        public Slot(StackItem[] items, IReferenceCounter referenceCounter)
        {
            _referenceCounter = referenceCounter;
            _items = items;
            foreach (StackItem item in items)
                referenceCounter.AddStackReference(item);
        }

        /// <summary>
        /// Create a slot of the specified size.
        /// </summary>
        /// <param name="count">Indicates the number of items contained in the slot.</param>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        public Slot(int count, IReferenceCounter referenceCounter)
        {
            _referenceCounter = referenceCounter;
            _items = new StackItem[count];
            Array.Fill(_items, StackItem.Null);
            referenceCounter.AddStackReference(StackItem.Null, count);
        }

        internal void ClearReferences()
        {
            foreach (StackItem item in _items)
                _referenceCounter.RemoveStackReference(item);
        }

        IEnumerator<StackItem> IEnumerable<StackItem>.GetEnumerator()
        {
            foreach (StackItem item in _items) yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
