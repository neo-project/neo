// Copyright (C) 2015-2025 The Neo Project.
//
// OptimizedEvaluationStack.cs file belongs to the neo project and is free
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
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Optimized array-based evaluation stack implementation for better performance.
    /// Uses a fixed-size array with dynamic resizing to avoid List overhead.
    /// </summary>
    public sealed class OptimizedEvaluationStack : EvaluationStack
    {
        private const int InitialCapacity = 16;
        private const int MaxArrayLength = 0x7FFFFFC7; // Same as List
        
        private StackItem[] items;
        private int count;

        /// <summary>
        /// Gets the number of items on the stack.
        /// </summary>
        public new int Count => count;

        internal OptimizedEvaluationStack(IReferenceCounter referenceCounter) : base(referenceCounter)
        {
            items = new StackItem[InitialCapacity];
        }

        internal new void Clear()
        {
            for (int i = 0; i < count; i++)
            {
                ReferenceCounter.RemoveStackReference(items[i]);
                items[i] = null!; // Clear reference for GC
            }
            count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int min)
        {
            if (items.Length < min)
            {
                int newCapacity = items.Length == 0 ? InitialCapacity : items.Length * 2;
                if ((uint)newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                
                var newItems = new StackItem[newCapacity];
                if (count > 0)
                {
                    System.Array.Copy(items, 0, newItems, 0, count);
                }
                items = newItems;
            }
        }

        internal new void CopyTo(EvaluationStack stack, int count = -1)
        {
            if (count < -1 || count > this.count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            
            int copyCount = count == -1 ? this.count : count;
            int startIndex = count == -1 ? 0 : this.count - count;
            
            // For now, use the public interface to maintain compatibility
            for (int i = 0; i < copyCount; i++)
            {
                var item = items[startIndex + i];
                stack.Push(item);
            }
        }

        public new IEnumerator<StackItem> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
                yield return items[i];
        }

        // Base class already implements IEnumerable interface

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal new void Insert(int index, StackItem item)
        {
            if (index > count) throw new InvalidOperationException($"Insert index is out of stack bounds: {index}/{count}");
            
            EnsureCapacity(count + 1);
            int insertAt = count - index;
            
            if (insertAt < count)
            {
                System.Array.Copy(items, insertAt, items, insertAt + 1, count - insertAt);
            }
            
            items[insertAt] = item;
            count++;
            ReferenceCounter.AddStackReference(item);
        }

        internal new void MoveTo(EvaluationStack stack, int count = -1)
        {
            if (count == 0) return;
            
            int moveCount = count == -1 ? this.count : count;
            int startIndex = count == -1 ? 0 : this.count - count;
            
            // For now, use the public interface to maintain compatibility
            for (int i = 0; i < moveCount; i++)
            {
                var item = items[startIndex + i];
                stack.Push(item);
                items[startIndex + i] = null!;
            }
            
            this.count -= moveCount;
        }

        /// <summary>
        /// Returns the item at the specified index from the top of the stack without removing it.
        /// </summary>
        /// <param name="index">The index of the object from the top of the stack.</param>
        /// <returns>The item at the specified index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new StackItem Peek(int index = 0)
        {
            if (index >= count) throw new InvalidOperationException($"Peek index is out of stack bounds: {index}/{count}");
            if (index < 0)
            {
                index += count;
                if (index < 0) throw new InvalidOperationException($"Peek index is out of stack bounds: {index}/{count}");
            }
            return items[count - index - 1];
        }

        // Base class already implements IReadOnlyList interface

        /// <summary>
        /// Pushes an item onto the top of the stack.
        /// </summary>
        /// <param name="item">The item to be pushed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new void Push(StackItem item)
        {
            if (count == items.Length)
            {
                EnsureCapacity(count + 1);
            }
            items[count++] = item;
            ReferenceCounter.AddStackReference(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal new void Reverse(int n)
        {
            if (n < 0 || n > count)
                throw new ArgumentOutOfRangeException(nameof(n));
            if (n <= 1) return;
            
            int start = count - n;
            int end = count - 1;
            while (start < end)
            {
                var temp = items[start];
                items[start] = items[end];
                items[end] = temp;
                start++;
                end--;
            }
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack.
        /// </summary>
        /// <returns>The item removed from the top of the stack.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new StackItem Pop()
        {
            if (count == 0)
                throw new InvalidOperationException("Stack is empty");
            
            var item = items[--count];
            items[count] = null!; // Clear reference for GC
            ReferenceCounter.RemoveStackReference(item);
            return item;
        }

        /// <summary>
        /// Removes and returns the item at the top of the stack and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The item removed from the top of the stack.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new T Pop<T>() where T : StackItem
        {
            return Remove<T>(0);
        }

        internal new T Remove<T>(int index) where T : StackItem
        {
            if (index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index < 0)
            {
                index += count;
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
            
            int removeAt = count - index - 1;
            if (items[removeAt] is not T item)
                throw new InvalidCastException($"The item can't be casted to type {typeof(T)}");
            
            count--;
            if (removeAt < count)
            {
                System.Array.Copy(items, removeAt + 1, items, removeAt, count - removeAt);
            }
            items[count] = null!; // Clear reference for GC
            
            ReferenceCounter.RemoveStackReference(item);
            return item;
        }

        public new string ToString()
        {
            if (count == 0) return "[]";
            
            var sb = new System.Text.StringBuilder();
            sb.Append('[');
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(", ");
                var item = items[i];
                sb.Append($"{item.Type}({item})");
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}