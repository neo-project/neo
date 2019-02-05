using System.Collections.Generic;
using Neo.Network.P2P.Payloads;

namespace Neo.Plugins
{
    public interface IMemoryPoolTxObserverPlugin
    {
        void AddedTransaction(Transaction tx);
        void RemovedTransactions(MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions);
    }
}
