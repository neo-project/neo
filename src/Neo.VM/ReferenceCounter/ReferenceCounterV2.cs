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

namespace Neo.VM
{
    /// <summary>
    /// Used for reference counting of objects in the VM.
    /// </summary>
    public sealed class ReferenceCounterV2 : IReferenceCounter
    {
        public RCVersion Version { get; } = RCVersion.V2;

        private readonly ExecutionEngineLimits _limits;

        // Keeps the total count of references.
        private int _referencesCount = 0;

        /// <inheritdoc/>
        public int Count => _referencesCount;

        public ReferenceCounterV2(ExecutionEngineLimits? limits = null)
        {
            _limits = limits ?? ExecutionEngineLimits.Default;
        }

        /// <inheritdoc/>
        public void AddReference(StackItem item, CompoundType parent)
        {
            // This call should not be made
        }

        /// <inheritdoc/>
        public void AddStackReference(StackItem item, int count = 1)
        {
            // Increment the reference count by the specified count.
            _referencesCount += count;

            if (_referencesCount > _limits.MaxStackSize)
                throw new IndexOutOfRangeException("Circular reference detected, execution stopped.");
            if (item is CompoundType compoundType)
            {
                // Increment the item's stack references by the specified count.
                compoundType.StackReferences += count;

                if (compoundType.StackReferences == count)
                {
                    foreach (var subItem in compoundType.SubItems)
                    {
                        AddStackReference(subItem);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void AddZeroReferred(StackItem item)
        {
            // This call should not be made
        }

        /// <inheritdoc/>
        public void CheckPostExecution()
        {
            if (Count > _limits.MaxStackSize)
                throw new InvalidOperationException($"MaxStackSize exceed: {Count}/{_limits.MaxStackSize}");
        }

        /// <inheritdoc/>
        public void RemoveReference(StackItem item, CompoundType parent)
        {
            // This call should not be made
        }

        /// <inheritdoc/>
        public void RemoveStackReference(StackItem item)
        {
            // Increment the reference count by the specified count.
            _referencesCount--;
            if (_referencesCount < 0)
                throw new IndexOutOfRangeException("Circular reference detected, execution stopped.");

            if (item is CompoundType compoundType)
            {
                // Decrease the item's stack references.
                compoundType.StackReferences--;

                if (compoundType.StackReferences < 0)
                    throw new IndexOutOfRangeException("Circular reference detected, execution stopped.");
                if (compoundType.StackReferences == 0)
                {
                    foreach (var subItem in compoundType.SubItems)
                    {
                        RemoveStackReference(subItem);
                    }
                }
            }
        }
    }
}
