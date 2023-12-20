// Copyright (C) 2015-2024 The Neo Project.
//
// IIterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Iterators
{
    /// <summary>
    /// Represents iterators in smart contract.
    /// </summary>
    public interface IIterator : IDisposable
    {
        /// <summary>
        /// Advances the iterator to the next element of the collection.
        /// </summary>
        /// <returns><see langword="true"/> if the iterator was successfully advanced to the next element; <see langword="false"/> if the iterator has passed the end of the collection.</returns>
        bool Next();

        /// <summary>
        /// Gets the element in the collection at the current position of the iterator.
        /// </summary>
        /// <returns>The element in the collection at the current position of the iterator.</returns>
        StackItem Value(ReferenceCounter referenceCounter);
    }
}
