// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Iterators;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Iterator.Next.
        /// Advances the iterator to the next element of the collection.
        /// </summary>
        public static readonly InteropDescriptor System_Iterator_Next = Register("System.Iterator.Next", nameof(IteratorNext), 1 << 15, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Iterator.Value.
        /// Gets the element in the collection at the current position of the iterator.
        /// </summary>
        public static readonly InteropDescriptor System_Iterator_Value = Register("System.Iterator.Value", nameof(IteratorValue), 1 << 4, CallFlags.None);

        /// <summary>
        /// The implementation of System.Iterator.Next.
        /// Advances the iterator to the next element of the collection.
        /// </summary>
        /// <param name="iterator">The iterator to be advanced.</param>
        /// <returns><see langword="true"/> if the iterator was successfully advanced to the next element; <see langword="false"/> if the iterator has passed the end of the collection.</returns>
        internal protected static bool IteratorNext(IIterator iterator)
        {
            return iterator.Next();
        }

        /// <summary>
        /// The implementation of System.Iterator.Value.
        /// Gets the element in the collection at the current position of the iterator.
        /// </summary>
        /// <param name="iterator">The iterator to be used.</param>
        /// <returns>The element in the collection at the current position of the iterator.</returns>
        internal protected StackItem IteratorValue(IIterator iterator)
        {
            return iterator.Value(ReferenceCounter);
        }
    }
}
