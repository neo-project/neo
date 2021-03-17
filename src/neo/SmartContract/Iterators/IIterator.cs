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
        StackItem Value();
    }
}
