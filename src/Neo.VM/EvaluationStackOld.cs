// Copyright (C) 2015-2025 The Neo Project.
//
// EvaluationStackOld.cs file belongs to the neo project and is free
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
    /// Original EvaluationStack implementation for benchmark comparison.
    /// </summary>
    public sealed class EvaluationStackOld : IReadOnlyList<StackItem>
    {
        private readonly List<StackItem> innerList = [];
        private readonly IReferenceCounter referenceCounter;

        internal IReferenceCounter ReferenceCounter => referenceCounter;

        internal EvaluationStackOld(IReferenceCounter referenceCounter)
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

        public IEnumerator<StackItem> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        /// <summary>
        /// Returns the item at the specified index from the top of the stack without removing it.
        /// </summary>
        /// <param name="index">The index of the object from the top of the stack.</param>
        /// <returns>The item at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            if (index >= innerList.Count) throw new InvalidOperationException($"Peek index is out of stack bounds: {index}/{innerList.Count}");
            if (index < 0)
            {
                index += innerList.Count;
                if (index < 0) throw new InvalidOperationException($"Peek index is out of stack bounds: {index}/{innerList.Count}");
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
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack (OLD IMPLEMENTATION).
        /// </summary>
        /// <returns>The item removed from the top of the stack.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Pop()
        {
            return Remove<StackItem>(0);
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack and convert it to the specified type (OLD IMPLEMENTATION).
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
    }
}
