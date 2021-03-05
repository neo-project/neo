using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList) { }
        void OnCommit(NeoSystem system, Block block, DataCache snapshot) { }
        bool ShouldThrowExceptionFromCommit(Exception ex) => false;
    }
}
