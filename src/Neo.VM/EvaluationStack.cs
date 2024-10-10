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
        private readonly List<StackItem> _innerList = [];
        private readonly IReferenceCounter _referenceCounter;

        internal IReferenceCounter ReferenceCounter => _referenceCounter;

        internal EvaluationStack(IReferenceCounter referenceCounter)
        {
            _referenceCounter = referenceCounter;
        }

        /// <summary>
        /// Gets the number of items on the stack.
        /// </summary>
        public int Count => _innerList.Count;

        internal void Clear()
        {
            foreach (var item in _innerList)
                _referenceCounter.RemoveStackReference(item);
            _innerList.Clear();
        }

        internal void CopyTo(EvaluationStack stack, int count = -1)
        {
            if (count < -1 || count > _innerList.Count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            if (count == -1 || count == _innerList.Count)
                stack._innerList.AddRange(_innerList);
            else
                stack._innerList.AddRange(_innerList.Skip(_innerList.Count - count));
        }

        public IEnumerator<StackItem> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Insert(int index, StackItem item)
        {
            if (index > _innerList.Count) throw new InvalidOperationException($"Insert out of bounds: {index}/{_innerList.Count}");
            _innerList.Insert(_innerList.Count - index, item);
            _referenceCounter.AddStackReference(item);
        }

        internal void MoveTo(EvaluationStack stack, int count = -1)
        {
            if (count == 0) return;
            CopyTo(stack, count);
            if (count == -1 || count == _innerList.Count)
                _innerList.Clear();
            else
                _innerList.RemoveRange(_innerList.Count - count, count);
        }

        /// <summary>
        /// Returns the item at the specified index from the top of the stack without removing it.
        /// </summary>
        /// <param name="index">The index of the object from the top of the stack.</param>
        /// <returns>The item at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StackItem Peek(int index = 0)
        {
            if (index >= _innerList.Count) throw new InvalidOperationException($"Peek out of bounds: {index}/{_innerList.Count}");
            if (index < 0)
            {
                index += _innerList.Count;
                if (index < 0) throw new InvalidOperationException($"Peek out of bounds: {index}/{_innerList.Count}");
            }
            return _innerList[_innerList.Count - index - 1];
        }

        StackItem IReadOnlyList<StackItem>.this[int index] => Peek(index);

        /// <summary>
        /// Pushes an item onto the top of the stack.
        /// </summary>
        /// <param name="item">The item to be pushed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(StackItem item)
        {
            _innerList.Add(item);
            _referenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reverse(int n)
        {
            if (n < 0 || n > _innerList.Count)
                throw new ArgumentOutOfRangeException(nameof(n));
            if (n <= 1) return;
            _innerList.Reverse(_innerList.Count - n, n);
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
            if (index >= _innerList.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index < 0)
            {
                index += _innerList.Count;
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
            index = _innerList.Count - index - 1;
            if (_innerList[index] is not T item)
                throw new InvalidCastException($"The item can't be casted to type {typeof(T)}");
            _innerList.RemoveAt(index);
            _referenceCounter.RemoveStackReference(item);
            return item;
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", _innerList.Select(p => $"{p.Type}({p})"))}]";
        }
    }
}
