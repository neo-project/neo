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

using Neo.VM.StronglyConnectedComponents;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Used for reference counting of objects in the VM.
    /// </summary>
    public sealed class ReferenceCounter : IReferenceCounter
    {
        // If set to true, all items will be tracked regardless of their type.
        private const bool TrackAllItems = false;

        // Stores items that are being tracked for references.
        // Only CompoundType and Buffer items are tracked.
        private readonly HashSet<StackItem> _trackedItems = new(ReferenceEqualityComparer.Instance);

        // Stores items that have zero references.
        private readonly HashSet<StackItem> _zeroReferred = new(ReferenceEqualityComparer.Instance);

        // Caches strongly connected components for optimization.
        private LinkedList<HashSet<StackItem>>? _cachedComponents;

        // Keeps the total count of references.
        private int _referencesCount = 0;

        /// <summary>
        /// Gets the count of references.
        /// </summary>
        public int Count => _referencesCount;

        /// <summary>
        /// Determines if an item needs to be tracked based on its type.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the item needs to be tracked, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedTrack(StackItem item)
        {
            // Track all items if TrackAllItems is true.
#pragma warning disable CS0162
            if (TrackAllItems) return true;
#pragma warning restore CS0162

            // Track the item if it is a CompoundType or Buffer.
            if (item is CompoundType or Buffer) return true;
            return false;
        }

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
        public void AddReference(StackItem item, CompoundType parent)
        {
            // Increment the reference count.
            _referencesCount++;

            // If the item doesn't need to be tracked, return early.
            // Only track CompoundType and Buffer items.
            if (!NeedTrack(item)) return;

            // Invalidate the cached components since the tracked items are changing.
            _cachedComponents = null;

            // Add the item to the set of tracked items.
            _trackedItems.Add(item);

            // Initialize the ObjectReferences dictionary if it is null.
            item.ObjectReferences ??= new Dictionary<CompoundType, StackItem.ObjectReferenceEntry>(ReferenceEqualityComparer.Instance);

            // Add the parent to the item's ObjectReferences dictionary and increment its reference count.
            if (!item.ObjectReferences.TryGetValue(parent, out var pEntry))
            {
                pEntry = new StackItem.ObjectReferenceEntry(parent);
                item.ObjectReferences.Add(parent, pEntry);
            }
            pEntry.References++;
        }

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
        public void AddStackReference(StackItem item, int count = 1)
        {
            // Increment the reference count by the specified count.
            _referencesCount += count;

            // If the item doesn't need to be tracked, return early.
            if (!NeedTrack(item)) return;

            // Add the item to the set of tracked items and to the cached components if needed.
            if (_trackedItems.Add(item))
                _cachedComponents?.AddLast(new HashSet<StackItem>(ReferenceEqualityComparer.Instance) { item });

            // Increment the item's stack references by the specified count.
            item.StackReferences += count;

            // Remove the item from the _zeroReferred set since it now has references.
            _zeroReferred.Remove(item);
        }

        /// <summary>
        /// Adds an item to the zero-referred list.
        ///
        /// This method is used when an item has no remaining references.
        /// It adds the item to the zero-referred list to be checked for cleanup later.
        ///
        /// Use this method when you detect that an item has zero references and may need to be cleaned up.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddZeroReferred(StackItem item)
        {
            // Add the item to the _zeroReferred set.
            _zeroReferred.Add(item);

            // If the item doesn't need to be tracked, return early.
            if (!NeedTrack(item)) return;

            // Add the item to the cached components and the set of tracked items.
            _cachedComponents?.AddLast(new HashSet<StackItem>(ReferenceEqualityComparer.Instance) { item });
            _trackedItems.Add(item);
        }

        /// <summary>
        /// Checks and processes items that have zero references.
        ///
        /// This method is used to check items in the zero-referred list and clean up those that are no longer needed.
        /// It uses Tarjan's algorithm to find strongly connected components and remove those with no references.
        ///
        /// Use this method periodically to clean up items with zero references and free up memory.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int CheckZeroReferred()
        {
            // If there are items with zero references, process them.
            if (_zeroReferred.Count > 0)
            {
                // Clear the zero_referred set since we are going to process all of them.
                _zeroReferred.Clear();

                // If cached components are null, we need to recompute the strongly connected components (SCCs).
                if (_cachedComponents is null)
                {
                    // Create a new Tarjan object and invoke it to find all SCCs in the tracked_items graph.
                    Tarjan tarjan = new(_trackedItems);
                    _cachedComponents = tarjan.Invoke();
                }

                // Reset all tracked items' Tarjan algorithm-related fields (DFN, LowLink, and OnStack).
                foreach (StackItem item in _trackedItems)
                    item.Reset();

                // Process each SCC in the cached_components list.
                for (var node = _cachedComponents.First; node != null;)
                {
                    var component = node.Value;
                    bool on_stack = false;

                    // Check if any item in the SCC is still on the stack.
                    foreach (StackItem item in component)
                    {
                        // An item is considered 'on stack' if it has stack references or if its parent items are still on stack.
                        if (item.StackReferences > 0 || item.ObjectReferences?.Values.Any(p => p.References > 0 && p.Item.OnStack) == true)
                        {
                            on_stack = true;
                            break;
                        }
                    }

                    // If any item in the component is on stack, mark all items in the component as on stack.
                    if (on_stack)
                    {
                        foreach (StackItem item in component)
                            item.OnStack = true;
                        node = node.Next;
                    }
                    else
                    {
                        // Otherwise, remove the component and clean up the items.
                        foreach (StackItem item in component)
                        {
                            _trackedItems.Remove(item);

                            // If the item is a CompoundType, adjust the reference count and clean up its sub-items.
                            if (item is CompoundType compound)
                            {
                                // Decrease the reference count by the number of sub-items.
                                _referencesCount -= compound.SubItemsCount;
                                foreach (StackItem subitem in compound.SubItems)
                                {
                                    // Skip sub-items that are in the same component or don't need tracking.
                                    if (component.Contains(subitem)) continue;
                                    if (!NeedTrack(subitem)) continue;

                                    // Remove the parent reference from the sub-item.
                                    subitem.ObjectReferences!.Remove(compound);
                                }
                            }

                            // Perform cleanup for the item.
                            item.Cleanup();
                        }

                        // Move to the next component and remove the current one from the cached_components list.
                        var nodeToRemove = node;
                        node = node.Next;
                        _cachedComponents.Remove(nodeToRemove);
                    }
                }
            }

            // Return the current total reference count.
            return _referencesCount;
        }


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
        public void RemoveReference(StackItem item, CompoundType parent)
        {
            // Decrement the reference count.
            _referencesCount--;

            // If the item doesn't need to be tracked, return early.
            if (!NeedTrack(item)) return;

            // Invalidate the cached components since the tracked items are changing.
            _cachedComponents = null;

            // Decrement the reference count for the parent in the item's ObjectReferences dictionary.
            item.ObjectReferences![parent].References--;

            // If the item has no stack references, add it to the zero_referred set.
            if (item.StackReferences == 0)
                _zeroReferred.Add(item);
        }

        /// <summary>
        /// Removes a stack reference from a specified item.
        ///
        /// This method is used when an item loses a stack reference, usually due to being popped off the evaluation stack.
        /// It decrements the reference count and updates the tracking structures if necessary.
        ///
        /// Use this method when you need to remove one or more stack references from a stack item.
        /// </summary>
        /// <param name="item">The item to remove a stack reference from.</param>
        public void RemoveStackReference(StackItem item)
        {
            // Decrement the reference count.
            _referencesCount--;

            // If the item doesn't need to be tracked, return early.
            if (!NeedTrack(item)) return;

            // Decrement the item's stack references and add it to the zero_referred set if it has no references.
            if (--item.StackReferences == 0)
                _zeroReferred.Add(item);
        }
    }
}
