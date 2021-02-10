using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public interface IMemoryPoolTxObserverPlugin
    {
        void TransactionAdded(NeoSystem system, Transaction tx);
        void TransactionsRemoved(NeoSystem system, MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions);
    }
}
