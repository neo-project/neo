// Copyright (C) 2015-2025 The Neo Project.
//
// Array.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents an array or a complex object in the VM.
    /// </summary>
    public class Array : CompoundType, IReadOnlyList<StackItem>
    {
        protected readonly List<StackItem> _array;

        /// <summary>
        /// Get or set item in the array.
        /// </summary>
        /// <param name="index">The index of the item in the array.</param>
        /// <returns>The item at the specified index.</returns>
        public StackItem this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array[index];
            set
            {
                if (IsReadOnly) throw new InvalidOperationException("The object is readonly.");
                ReferenceCounter?.RemoveReference(_array[index], this);
                _array[index] = value;
                if (ReferenceCounter != null && value is CompoundType { ReferenceCounter: null })
                {
                    throw new InvalidOperationException("Can not set a CompoundType without a ReferenceCounter.");
                }

                ReferenceCounter?.AddReference(value, this);
            }
        }

        /// <summary>
        /// The number of items in the array.
        /// </summary>
        public override int Count => _array.Count;
        public override IEnumerable<StackItem> SubItems => _array;
        public override int SubItemsCount => _array.Count;
        public override StackItemType Type => StackItemType.Array;

        /// <summary>
        /// Create an array containing the specified items.
        /// </summary>
        /// <param name="items">The items to be included in the array.</param>
        public Array(IEnumerable<StackItem>? items = null)
            : this(null, items)
        {
        }

        /// <summary>
        /// Create an array containing the specified items. And make the array use the specified <see cref="IReferenceCounter"/>.
        /// </summary>
        /// <param name="referenceCounter">The <see cref="IReferenceCounter"/> to be used by this array.</param>
        /// <param name="items">The items to be included in the array.</param>
        public Array(IReferenceCounter? referenceCounter, IEnumerable<StackItem>? items = null)
            : base(referenceCounter)
        {
            _array = items switch
            {
                null => new List<StackItem>(),
                List<StackItem> list => list,
                _ => new List<StackItem>(items)
            };

            if (referenceCounter == null) return;

            foreach (var item in _array)
            {
                if (item is CompoundType { ReferenceCounter: null })
                {
                    throw new InvalidOperationException("Can not set a CompoundType without a ReferenceCounter.");
                }

                referenceCounter.AddReference(item, this);
            }
        }

        /// <summary>
        /// Add a new item at the end of the array.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        public void Add(StackItem item)
        {
            if (IsReadOnly) throw new InvalidOperationException("The array is readonly, can not add item.");
            _array.Add(item);

            if (ReferenceCounter == null) return;

            if (item is CompoundType { ReferenceCounter: null })
            {
                throw new InvalidOperationException("Can not set a CompoundType without a ReferenceCounter.");
            }
            ReferenceCounter.AddReference(item, this);
        }

        public override void Clear()
        {
            if (IsReadOnly) throw new InvalidOperationException("The array is readonly, can not clear.");
            if (ReferenceCounter != null)
                foreach (StackItem item in _array)
                    ReferenceCounter.RemoveReference(item, this);
            _array.Clear();
        }

        public override StackItem ConvertTo(StackItemType type)
        {
            if (Type == StackItemType.Array && type == StackItemType.Struct)
                return new Struct(ReferenceCounter, new List<StackItem>(_array));
            return base.ConvertTo(type);
        }

        internal sealed override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable)
        {
            if (refMap.TryGetValue(this, out StackItem? mappedItem)) return mappedItem;
            Array result = this is Struct ? new Struct(ReferenceCounter) : new Array(ReferenceCounter);
            refMap.Add(this, result);
            foreach (StackItem item in _array)
                result.Add(item.DeepCopy(refMap, asImmutable));
            result.IsReadOnly = true;
            return result;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        /// <summary>
        /// Remove the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to be removed.</param>
        public void RemoveAt(int index)
        {
            if (IsReadOnly) throw new InvalidOperationException("The array is readonly, can not remove item.");
            ReferenceCounter?.RemoveReference(_array[index], this);
            _array.RemoveAt(index);
        }

        /// <summary>
        /// Reverse all items in the array.
        /// </summary>
        public void Reverse()
        {
            if (IsReadOnly) throw new InvalidOperationException("The array is readonly, can not reverse.");
            _array.Reverse();
        }
    }
}
