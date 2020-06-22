using System;
using System.Collections.Generic;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Plugins
{
    public interface ITraceDebugSink : IDisposable
    {
        void Trace(VMState vmState, IReadOnlyCollection<ExecutionContext> stackFrames, 
            IEnumerable<(UInt160 scriptHash, byte[] key, StorageItem item)> storages);
        void Log(LogEventArgs args);
        void Notify(NotifyEventArgs args);
        void Results(VMState vmState, long gasConsumed, IReadOnlyCollection<Neo.VM.Types.StackItem> results);
    }
}
