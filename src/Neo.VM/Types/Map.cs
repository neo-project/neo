// Copyright (C) 2015-2025 The Neo Project.
//
// Map.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents an ordered collection of key-value pairs in the VM.
    /// </summary>
    public class Map : CompoundType, IReadOnlyDictionary<PrimitiveType, StackItem>
    {
        /// <summary>
        /// Indicates the maximum size of keys in bytes.
        /// </summary>
        public const int MaxKeySize = 64;

        private readonly Collections.OrderedDictionary<PrimitiveType, StackItem> dictionary = new();

        /// <summary>
        /// Gets or sets the element that has the specified key in the map.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>The element that has the specified key in the map.</returns>
        public StackItem this[PrimitiveType key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (key.Size > MaxKeySize)
                    throw new ArgumentException($"Can not get value from map, MaxKeySize of {nameof(Map)} is exceeded: {key.Size}/{MaxKeySize}.");
                return dictionary[key];
            }
            set
            {
                if (key.Size > MaxKeySize)
                    throw new ArgumentException($"Can not set value to map, MaxKeySize of {nameof(Map)} is exceeded: {key.Size}/{MaxKeySize}.");
                if (IsReadOnly) throw new InvalidOperationException("The map is readonly, can not set value.");
                if (ReferenceCounter != null)
                {
                    if (dictionary.TryGetValue(key, out StackItem? old_value))
                        ReferenceCounter.RemoveReference(old_value, this);
                    else
                        ReferenceCounter.AddReference(key, this);
                    if (value is CompoundType { ReferenceCounter: null })
                    {
                        throw new InvalidOperationException("Can not set a Map without a ReferenceCounter.");
                    }
                    ReferenceCounter.AddReference(value, this);
                }
                dictionary[key] = value;
            }
        }

        public override int Count => dictionary.Count;

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the map.
        /// </summary>
        public IEnumerable<PrimitiveType> Keys => dictionary.Keys;

        public override IEnumerable<StackItem> SubItems => Keys.Concat(Values);

        public override int SubItemsCount => dictionary.Count * 2;

        public override StackItemType Type => StackItemType.Map;

        /// <summary>
        /// Gets an enumerable collection that contains the values in the map.
        /// </summary>
        public IEnumerable<StackItem> Values => dictionary.Values;

        /// <summary>
        /// Create a new map with the specified reference counter.
        /// </summary>
        /// <param name="referenceCounter">The reference counter to be used.</param>
        public Map(IReferenceCounter? referenceCounter = null)
            : base(referenceCounter)
        {
        }

        public override void Clear()
        {
            if (IsReadOnly) throw new InvalidOperationException("The map is readonly, can not clear.");
            if (ReferenceCounter != null)
                foreach (var pair in dictionary)
                {
                    ReferenceCounter.RemoveReference(pair.Key, this);
                    ReferenceCounter.RemoveReference(pair.Value, this);
                }
            dictionary.Clear();
        }

        /// <summary>
        /// Determines whether the map contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>
        /// <see langword="true" /> if the map contains an element that has the specified key;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool ContainsKey(PrimitiveType key)
        {
            if (key.Size > MaxKeySize)
                throw new ArgumentException($"Can not check if map contains key, MaxKeySize of {nameof(Map)} is exceeded: {key.Size}/{MaxKeySize}.");
            return dictionary.ContainsKey(key);
        }

        internal override StackItem DeepCopy(Dictionary<StackItem, StackItem> refMap, bool asImmutable)
        {
            if (refMap.TryGetValue(this, out StackItem? mappedItem)) return mappedItem;
            Map result = new(ReferenceCounter);
            refMap.Add(this, result);
            foreach (var (k, v) in dictionary)
                result[k] = v.DeepCopy(refMap, asImmutable);
            result.IsReadOnly = true;
            return result;
        }

        IEnumerator<KeyValuePair<PrimitiveType, StackItem>> IEnumerable<KeyValuePair<PrimitiveType, StackItem>>.GetEnumerator()
        {
            return ((IDictionary<PrimitiveType, StackItem>)dictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<PrimitiveType, StackItem>)dictionary).GetEnumerator();
        }

        /// <summary>
        /// Removes the element with the specified key from the map.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// <see langword="true" /> if the element is successfully removed;
        /// otherwise, <see langword="false"/>.
        /// This method also returns <see langword="false"/> if <paramref name="key"/> was not found in the original map.
        /// </returns>
        public bool Remove(PrimitiveType key)
        {
            if (key.Size > MaxKeySize)
                throw new ArgumentException($"Can not remove key from map, MaxKeySize of {nameof(Map)} is exceeded: {key.Size}/{MaxKeySize}.");
            if (IsReadOnly) throw new InvalidOperationException("The map is readonly, can not remove key.");
            if (!dictionary.Remove(key, out StackItem? old_value))
                return false;
            ReferenceCounter?.RemoveReference(key, this);
            ReferenceCounter?.RemoveReference(old_value, this);
            return true;
        }

        /// <summary>
        /// Gets the value that is associated with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found;
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if the map contains an element that has the specified key;
        /// otherwise, <see langword="false"/>.
        /// </returns>
// supress warning of value parameter nullability mismatch
#pragma warning disable CS8767
        public bool TryGetValue(PrimitiveType key, [MaybeNullWhen(false)] out StackItem value)
#pragma warning restore CS8767
        {
            if (key.Size > MaxKeySize)
                throw new ArgumentException($"Can not get value from map, MaxKeySize of {nameof(Map)} is exceeded: {key.Size}/{MaxKeySize}.");
            return dictionary.TryGetValue(key, out value);
        }
    }
}
