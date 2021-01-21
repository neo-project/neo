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

        private StackItem _rawTransaction;

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            BlockIndex = (uint)@struct[0].GetInteger();
            _rawTransaction = @struct[1];
            Transaction = _rawTransaction.GetSpan().AsSerializable<Transaction>();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            _rawTransaction ??= Transaction.ToArray();
            return new Struct(referenceCounter) { BlockIndex, _rawTransaction };
        }
    }
}
