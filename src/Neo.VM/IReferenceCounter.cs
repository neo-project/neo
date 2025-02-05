// Copyright (C) 2015-2025 The Neo Project.
//
// IReferenceCounter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM
{
    /// <summary>
    /// Used for reference counting of objects in the VM.
    /// </summary>
    public interface IReferenceCounter
    {
        /// <summary>
        /// Gets the count of references.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds an item to the zero-referred list.
        ///
        /// This method is used when an item has no remaining references.
        /// It adds the item to the zero-referred list to be checked for cleanup later.
        ///
        /// Use this method when you detect that an item has zero references and may need to be cleaned up.
        /// </summary>
        /// <param name="item">The item to add.</param>
        void AddZeroReferred(StackItem item);

        /// <summary>
        /// Adds a reference to a specified item with a parent compound type.
        ///
        /// This method is used when an item gains a new reference through a parent compound type.
        /// It increments the reference count and updates the tracking structures if necessary.
        ///
        /// Use this method when you need to add a reference from a compound type to a stack item.
        /// </summary>
        /// <param name="item">The item to add a reference to.</param>
        /// <param name="parent">The parent compound type.</param>
        void AddReference(StackItem item, CompoundType parent);

        /// <summary>
        /// Adds a stack reference to a specified item with a count.
        ///
        /// This method is used when an item gains a new stack reference, usually due to being pushed onto the evaluation stack.
        /// It increments the reference count and updates the tracking structures if necessary.
        ///
        /// Use this method when you need to add one or more stack references to a stack item.
        /// </summary>
        /// <param name="item">The item to add a stack reference to.</param>
        /// <param name="count">The number of references to add.</param>
        void AddStackReference(StackItem item, int count = 1);

        /// <summary>
        /// Removes a reference from a specified item with a parent compound type.
        ///
        /// This method is used when an item loses a reference from a parent compound type.
        /// It decrements the reference count and updates the tracking structures if necessary.
        ///
        /// Use this method when you need to remove a reference from a compound type to a stack item.
        /// </summary>
        /// <param name="item">The item to remove a reference from.</param>
        /// <param name="parent">The parent compound type.</param>
        void RemoveReference(StackItem item, CompoundType parent);

        /// <summary>
        /// Removes a stack reference from a specified item.
        ///
        /// This method is used when an item loses a stack reference, usually due to being popped off the evaluation stack.
        /// It decrements the reference count and updates the tracking structures if necessary.
        ///
        /// Use this method when you need to remove one or more stack references from a stack item.
        /// </summary>
        /// <param name="item">The item to remove a stack reference from.</param>
        void RemoveStackReference(StackItem item);

        /// <summary>
        /// Checks and processes items that have zero references.
        /// This method is used to check items in the zero-referred list and clean up those that are no longer needed.
        /// </summary>
        /// <returns>The current reference count.</returns>
        int CheckZeroReferred();
    }
}
