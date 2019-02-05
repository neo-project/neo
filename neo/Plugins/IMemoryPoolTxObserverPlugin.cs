using System.Collections.Generic;
using Neo.Network.P2P.Payloads;

namespace Neo.Plugins
{
    public interface IMemoryPoolTxObserverPlugin
    {
        bool AddedTransaction(Transaction tx);
        bool RemovedTransactions(MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions);
    }
}