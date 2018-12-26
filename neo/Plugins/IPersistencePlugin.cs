using Neo.Ledger;
using Neo.Persistence;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public interface IPersistencePlugin
    {
        void OnPersist(Snapshot snapshot, List<ApplicationExecutionResult> application);
    }
}
