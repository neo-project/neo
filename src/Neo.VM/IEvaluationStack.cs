// Copyright (C) 2015-2025 The Neo Project.
//
// IEvaluationStack.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.VM
{
    /// <summary>
    /// Interface for evaluation stack implementations.
    /// </summary>
    public interface IEvaluationStack : IReadOnlyList<StackItem>
    {
        /// <summary>
        /// The reference counter used by this stack.
        /// </summary>
        IReferenceCounter ReferenceCounter { get; }

        /// <summary>
        /// Clears all items from the stack.
        /// </summary>
        void Clear();

        /// <summary>
        /// Copies items to another stack.
        /// </summary>
        /// <param name="stack">The destination stack.</param>
        /// <param name="count">Number of items to copy, or -1 for all.</param>
        void CopyTo(IEvaluationStack stack, int count = -1);

        /// <summary>
        /// Moves items to another stack.
        /// </summary>
        /// <param name="stack">The destination stack.</param>
        /// <param name="count">Number of items to move, or -1 for all.</param>
        void MoveTo(IEvaluationStack stack, int count = -1);

        /// <summary>
        /// Inserts an item at the specified position.
        /// </summary>
        /// <param name="index">The index from the top of the stack.</param>
        /// <param name="item">The item to insert.</param>
        void Insert(int index, StackItem item);

        /// <summary>
        /// Returns the item at the specified index from the top of the stack.
        /// </summary>
        /// <param name="index">The index from the top of the stack.</param>
        /// <returns>The item at the specified index.</returns>
        StackItem Peek(int index = 0);

        /// <summary>
        /// Pushes an item onto the top of the stack.
        /// </summary>
        /// <param name="item">The item to push.</param>
        void Push(StackItem item);

        /// <summary>
        /// Removes and returns the item at the top of the stack.
        /// </summary>
        /// <returns>The item removed from the top of the stack.</returns>
        StackItem Pop();

        /// <summary>
        /// Removes and returns the item at the top of the stack and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The item removed from the top of the stack.</returns>
        T Pop<T>() where T : StackItem;

        /// <summary>
        /// Reverses the order of the top n items on the stack.
        /// </summary>
        /// <param name="n">The number of items to reverse.</param>
        void Reverse(int n);
    }
}