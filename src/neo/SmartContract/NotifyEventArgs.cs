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
        public string EventName { get; }
        public Array State { get; }

        public NotifyEventArgs(IVerifiable container, UInt160 script_hash, string eventName, Array state)
        {
            this.ScriptContainer = container;
            this.ScriptHash = script_hash;
            this.EventName = eventName;
            this.State = state;
        }

        public void FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter)
            {
                ScriptHash.ToArray(),
                EventName,
                State.DeepCopy()
            };
        }
    }
}
