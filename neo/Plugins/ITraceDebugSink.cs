using System;
using System.Collections.Generic;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Plugins
{
    public interface ITraceDebugSink : IDisposable
    {
        public readonly struct StackFrame
        {
            public readonly int StackFrameIndex;
            public readonly UInt160 ScriptHash;
            public readonly int InstructionPointer;
            public readonly Neo.VM.Types.Array Variables;
            public readonly IEnumerable<KeyValuePair<StorageKey, StorageItem>> Storages;

            public StackFrame(int stackFrameIndex, UInt160 scriptHash, int instructionPointer, VM.Types.Array variables, IEnumerable<KeyValuePair<StorageKey, StorageItem>> storages)
            {
                StackFrameIndex = stackFrameIndex;
                ScriptHash = scriptHash;
                InstructionPointer = instructionPointer;
                Variables = variables;
                Storages = storages;
            }
        }

        void Trace(VMState vmState, IList<StackFrame> stackFrames);
        void Log(LogEventArgs args);
        void Notify(NotifyEventArgs args);
        void Results(VMState vmState, Fixed8 gasConsumed, RandomAccessStack<StackItem> results);
    }

    public interface ITraceDebugPlugin
    {
        bool ShouldTrace(Header header, InvocationTransaction tx);
        
        ITraceDebugSink GetSink(Header header, InvocationTransaction tx);
    }
}
