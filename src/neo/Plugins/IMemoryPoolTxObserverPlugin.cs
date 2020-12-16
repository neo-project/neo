using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public interface IMemoryPoolTxObserverPlugin
    {
        void TransactionAdded(Transaction tx);
        void TransactionsRemoved(MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions);
    }
}
