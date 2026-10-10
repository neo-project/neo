// Copyright (C) 2015-2026 The Neo Project.
//
// ApplicationEngine.Iterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
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
        /// The price of System.Iterator.Value per reference pushed onto the stack since Huyao hardfork,
        /// in the unit of 1e-11 GAS.
        /// </summary>
        private const long IteratorValuePricePerRef = 727;

        /// <summary>
        /// The price of System.Iterator.Value per deserialized byte since Huyao hardfork, in the unit of 1e-11 GAS.
        /// </summary>
        private const long IteratorValuePricePerByte = 8;

        /// <summary>
        /// The base price of System.Iterator.Value since Huyao hardfork, in the unit of 1e-11 GAS.
        /// </summary>
        private const long IteratorValueBasePrice = 19173;

        /// <summary>
        /// The implementation of System.Iterator.Next.
        /// Advances the iterator to the next element of the collection.
        /// </summary>
        /// <param name="iterator">The iterator to be advanced.</param>
        /// <returns><see langword="true"/> if the iterator was successfully advanced to the next element; <see langword="false"/> if the iterator has passed the end of the collection.</returns>
        [InteropPrice(Hardfork.HF_Huyao, 3367)]
        internal protected static bool IteratorNext(IIterator iterator)
        {
            return iterator.Next();
        }

        /// <summary>
        /// The implementation of System.Iterator.Value.
        /// Gets the element in the collection at the current position of the iterator.
        /// </summary>
        /// <param name="iterator">The iterator to be used.</param>
        [InteropPrice(Hardfork.HF_Huyao, 0)]
        internal protected void IteratorValue(IIterator iterator)
        {
            var value = iterator.Value();
            var refs = ReferenceCounter.Count;
            Push(value);
            if (IsHardforkEnabledAtPersistingIndex(Hardfork.HF_Huyao))
            {
                var bytes = iterator is StorageIterator storageIterator ? storageIterator.DeserializedLength : 0;
                var price = IteratorValuePricePerRef * (ReferenceCounter.Count - refs) + IteratorValuePricePerByte * bytes + IteratorValueBasePrice;
                AddFemtoGas(price * _execFeeFactor, false);
            }
        }
    }
}
