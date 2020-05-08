using System;
using System.Collections.Generic;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Plugins
{
    public interface ITraceDebugSink : IDisposable
    {
        void Trace(VMState vmState, RandomAccessStack<ExecutionContext> contexts, 
            Func<UInt160, IEnumerable<KeyValuePair<StorageKey, StorageItem>>> getStorage);
        void Log(LogEventArgs args);
        void Notify(NotifyEventArgs args);
        void Results(VMState vmState, Fixed8 gasConsumed, RandomAccessStack<StackItem> results);
    }
}
