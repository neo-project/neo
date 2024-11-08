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
        private readonly List<StackItem> _stack = [];

        /// <summary>
        /// Gets the count of references.
        /// </summary>
        public int Count => _stack.Count;

        /// <summary>
        /// Adds item to Reference Counter
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="count">Number of similar entries</param>
        public void AddStackReference(StackItem item, int count = 1)
        {
            for (var x = 0; x < count; x++)
            {
                if (item is CompoundType compound && ReferenceEqualsIndexOf(item) == -1)
                {
                    // Add sub items only if it was not present

                    foreach (var subItem in compound.SubItems)
                    {
                        AddStackReference(subItem);
                    }
                }

                _stack.Add(item);
            }
        }

        /// <summary>
        /// Removes item from Reference Counter
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void RemoveStackReference(StackItem item)
        {
            var indexOf = ReferenceEqualsIndexOf(item);

            if (indexOf == -1)
            {
                throw new InvalidOperationException("Reference was not added before");
            }

            _stack.RemoveAt(indexOf);

            if (item is CompoundType compound && ReferenceEqualsIndexOf(item, indexOf) == -1)
            {
                // Remove all the childrens only if the compound is not present

                foreach (var subItem in compound.SubItems)
                {
                    RemoveStackReference(subItem);
                }
            }
        }

        private int ReferenceEqualsIndexOf(StackItem item, int index = 0)
        {
            // Note: List use Equals, and Struct don't allow to use it, so we iterate over the list

            for (; index < _stack.Count; index++)
            {
                if (ReferenceEquals(_stack[index], item))
                {
                    return index;
                }
            }

            return -1;
        }

        public void AddReference(StackItem item, CompoundType parent)
        {
            if (ReferenceEqualsIndexOf(parent) != -1)
            {
                // Add only if the parent is present

                AddStackReference(item);
            }
        }

        public void RemoveReference(StackItem item, CompoundType parent)
        {
            if (ReferenceEqualsIndexOf(parent) != -1)
            {
                // Remove only if the parent is present

                RemoveStackReference(item);
            }
        }

        public void AddZeroReferred(StackItem item)
        {
            // This version don't use this method
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
