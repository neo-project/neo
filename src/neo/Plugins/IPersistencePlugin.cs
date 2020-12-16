using Neo.Persistence;
using System;
using System.Collections.Generic;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(StoreView snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList) { }
        void OnCommit(StoreView snapshot) { }
        bool ShouldThrowExceptionFromCommit(Exception ex) => false;
    }
}
