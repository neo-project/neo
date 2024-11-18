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

using Neo.VM.StronglyConnectedComponents;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.VM
{
    /// <summary>
    /// Used for reference counting of objects in the VM.
    /// </summary>
    public sealed class ReferenceCounterV2 : IReferenceCounter
    {
        public RCVersion Version { get; init; } = RCVersion.V2;

        private readonly ExecutionEngineLimits _limits = ExecutionEngineLimits.Default;

        // Keeps the total count of references.
        private int _referencesCount = 0;

        /// <inheritdoc/>
        public int Count => _referencesCount;

        public ReferenceCounterV2(ExecutionEngineLimits limits = null)
        {
            _limits = limits ?? ExecutionEngineLimits.Default;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedTrack(StackItem item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void AddReference(StackItem item, CompoundType parent)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public int CheckZeroReferred()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void RemoveReference(StackItem item, CompoundType parent)
        {
            throw new NotImplementedException();
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
                // Increment the item's stack references by the specified count.
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
