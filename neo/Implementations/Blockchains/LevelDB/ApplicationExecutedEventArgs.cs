using Neo.Core;
using System;

namespace Neo.Implementations.Blockchains.LevelDB
{
    public class ApplicationExecutedEventArgs : EventArgs
    {
        public Transaction Transaction { get; internal set; }
        public ApplicationExecutionResult[] ExecutionResults { get; internal set; }
    }
}
