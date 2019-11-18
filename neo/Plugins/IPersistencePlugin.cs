using System;
using Neo.Persistence;
using System.Collections.Generic;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(SnapshotView snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList);
        void OnCommit(SnapshotView snapshot);
        bool ShouldThrowExceptionFromCommit(Exception ex);
    }
}
