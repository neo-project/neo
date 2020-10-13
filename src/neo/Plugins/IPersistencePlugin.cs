using System;
using Neo.Persistence;
using System.Collections.Generic;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(StoreView snapshot,
            IReadOnlyList<ApplicationExecuted> preApplicationExecutedList,
            IReadOnlyList<ApplicationExecuted> applicationExecutedList,
            IReadOnlyList<ApplicationExecuted> postApplicationExecutedList)
        { }
        void OnCommit(StoreView snapshot) { }
        bool ShouldThrowExceptionFromCommit(Exception ex) => false;
    }
}
