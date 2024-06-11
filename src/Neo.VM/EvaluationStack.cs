// Copyright (C) 2015-2024 The Neo Project.
//
// EvaluationStack.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Represents the evaluation stack in the VM.
    /// </summary>
    public sealed class EvaluationStack : IReadOnlyList<StackItem>
    {
        private readonly List<StackItem> innerList = new();
        private readonly ReferenceCounter referenceCounter;

        internal EvaluationStack(ReferenceCounter referenceCounter)
        {
            this.referenceCounter = referenceCounter;
        }

        /// <summary>
        /// Gets the number of items on the stack.
        /// </summary>
        public int Count => innerList.Count;

        internal void Clear()
        {
            foreach (StackItem item in innerList)
                referenceCounter.RemoveStackReference(item);
            innerList.Clear();
        }

        internal void CopyTo(EvaluationStack stack, int count = -1)
        {
            if (count < -1 || count > innerList.Count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            if (count == -1 || count == innerList.Count)
                stack.innerList.AddRange(innerList);
            else
                stack.innerList.AddRange(innerList.Skip(innerList.Count - count));
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Insert(int index, StackItem item)
        {
            if (index > innerList.Count) throw new InvalidOperationException($"Insert out of bounds: {index}/{innerList.Count}");
            innerList.Insert(innerList.Count - index, item);
            referenceCounter.AddStackReference(item);
        }

        internal void MoveTo(EvaluationStack stack, int count = -1)
        {
            if (count == 0) return;
            CopyTo(stack, count);
            if (count == -1 || count == innerList.Count)
                innerList.Clear();
            else
                innerList.RemoveRange(innerList.Count - count, count);
        }

        /// <summary>
        /// Returns the item at the specified index from the top of the stack without removing it.
        /// </summary>
        /// <param name="index">The index of the object from the top of the stack.</param>
        /// <returns>The item at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            if (index >= innerList.Count) throw new InvalidOperationException($"Peek out of bounds: {index}/{innerList.Count}");
            if (index < 0)
            {
                index += innerList.Count;
                if (index < 0) throw new InvalidOperationException($"Peek out of bounds: {index}/{innerList.Count}");
            }
            return innerList[innerList.Count - index - 1];
        }

        StackItem IReadOnlyList<StackItem>.this[int index] => Peek(index);

        /// <summary>
        /// Pushes an item onto the top of the stack.
        /// </summary>
        /// <param name="item">The item to be pushed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackItem item)
        {
            innerList.Add(item);
            referenceCounter.AddStackReference(item);
            if (item is CompoundType compoundType)
                CheckCompoundType(compoundType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reverse(int n)
        {
            if (n < 0 || n > innerList.Count)
                throw new ArgumentOutOfRangeException(nameof(n));
            if (n <= 1) return;
            innerList.Reverse(innerList.Count - n, n);
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack.
        /// </summary>
        /// <returns>The item removed from the top of the stack.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Pop()
        {
            return Remove<StackItem>(0);
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The item removed from the top of the stack.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop<T>() where T : StackItem
        {
            return Remove<T>(0);
        }

        internal T Remove<T>(int index) where T : StackItem
        {
            if (index >= innerList.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index < 0)
            {
                index += innerList.Count;
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
            index = innerList.Count - index - 1;
            if (innerList[index] is not T item)
                throw new InvalidCastException($"The item can't be casted to type {typeof(T)}");
            innerList.RemoveAt(index);
            referenceCounter.RemoveStackReference(item);
            return item;
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", innerList.Select(p => $"{p.Type}({p})"))}]";
        }

        private static void CheckCompoundType(CompoundType rootItem, int maxItems = 2048)
        {
            if (rootItem is null)
                throw new ArgumentNullException();

            var stack = new Stack<CompoundType>();
            var visited = new HashSet<CompoundType>(ReferenceEqualityComparer.Instance);
            int itemCount = 0;

            // Initialize the stack and visited set with the root item
            stack.Push(rootItem);
            visited.Add(rootItem);
            itemCount++;

            while (stack.Count > 0)
            {
                var currentCompound = stack.Pop();

                foreach (var subItem in currentCompound.SubItems)
                {
                    // Check if the subItem is not on the stack
                    if (!subItem.OnStack)
                    {
                        throw new InvalidOperationException("Invalid stackitem being pushed.");
                    }

                    // If the subItem is a CompoundType, process it
                    if (subItem is CompoundType compoundSubItem)
                    {
                        // Check for cycle dependency
                        if (!visited.Add(compoundSubItem))
                        {
                            throw new InvalidOperationException("Cycle detected in CompoundType structure.");
                        }

                        // Add the subItem to the stack and increment the itemCount
                        stack.Push(compoundSubItem);
                        itemCount++;

                        // Check if the itemCount exceeds the maximum allowed items
                        if (itemCount > maxItems)
                        {
                            throw new InvalidOperationException($"Exceeded maximum of {maxItems} items.");
                        }
                    }
                }
            }
        }
    }
}
