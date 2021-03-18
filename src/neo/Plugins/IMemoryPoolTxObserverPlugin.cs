using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.Plugins
{
    /// <summary>
    /// An interface that allows plugins to observe changes in the memory pool.
    /// </summary>
    public interface IMemoryPoolTxObserverPlugin
    {
        /// <summary>
        /// Called when a transaction is added to the memory pool.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the memory pool.</param>
        /// <param name="tx">The transaction added.</param>
        void TransactionAdded(NeoSystem system, Transaction tx);

        /// <summary>
        /// Called when transactions are removed from the memory pool.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the memory pool.</param>
        /// <param name="reason">The reason the transactions were removed.</param>
        /// <param name="transactions">The removed transactions.</param>
        void TransactionsRemoved(NeoSystem system, MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions);
    }
}
