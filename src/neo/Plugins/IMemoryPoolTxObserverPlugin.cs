using System.Collections.Generic;
using Neo.Network.P2P.Payloads;

namespace Neo.Plugins
{
    public interface IMemoryPoolTxObserverPlugin
    {
        int Order { get; }
        void TransactionAdded(Transaction tx);
        void TransactionsRemoved(MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions);
    }
}
