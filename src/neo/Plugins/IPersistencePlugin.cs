using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(Block block, DataCache snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList) { }
        void OnCommit(Block block, DataCache snapshot) { }
        bool ShouldThrowExceptionFromCommit(Exception ex) => false;
    }
}
