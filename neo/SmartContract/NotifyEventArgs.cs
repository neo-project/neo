using Neo.VM;
using System;

namespace Neo.SmartContract
{
    public class NotifyEventArgs : EventArgs
    {
        public IScriptContainer ScriptContainer { get; }
        public UInt160 ScriptHash { get; }
        public StackItem State { get; }

        public NotifyEventArgs(IScriptContainer container, UInt160 script_hash, StackItem state)
        {
            this.ScriptContainer = container;
            this.ScriptHash = script_hash;
            this.State = state;
        }
    }
}
