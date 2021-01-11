using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    public class TransactionState : IInteroperable
    {
        public uint BlockIndex;
        public Transaction Transaction;

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            BlockIndex = (uint)@struct[0].GetInteger();
            Transaction = @struct[1].GetSpan().AsSerializable<Transaction>();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { BlockIndex, Transaction.ToArray() };
        }
    }
}
