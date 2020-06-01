using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    public class NotifyEventArgs : EventArgs, IInteroperable
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

        public void FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new[] { ScriptHash.ToArray(), State });
        }
    }
}
