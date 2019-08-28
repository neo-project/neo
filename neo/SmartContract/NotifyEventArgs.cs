using Neo.Network.P2P.Payloads;
using Neo.VM;
using System;

namespace Neo.SmartContract
{
    public class NotifyEventArgs : EventArgs
    {
        public IVerifiable ScriptContainer { get; }
        public UInt160 ScriptHash { get; }
        public StackItem State { get; }

        public NotifyEventArgs(IVerifiable container, UInt160 script_hash, StackItem state)
        {
            this.ScriptContainer = container;
            this.ScriptHash = script_hash;
            this.State = state;
        }
    }
}
