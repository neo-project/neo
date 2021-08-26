using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents a transaction that has been included in a block.
    /// </summary>
    public class TransactionState : IInteroperable
    {
        /// <summary>
        /// The block containing this transaction.
        /// </summary>
        public uint BlockIndex;

        /// <summary>
        /// The transaction.
        /// </summary>
        public Transaction Transaction;

        /// <summary>
        /// The execution state
        /// </summary>
        public VMState State;

        private StackItem _rawTransaction;

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            BlockIndex = (uint)@struct[0].GetInteger();
            _rawTransaction = @struct[1];
            Transaction = _rawTransaction.GetSpan().AsSerializable<Transaction>();
            State = @struct.Count == 2 ? VMState.NONE : (VMState)(byte)@struct[2].GetInteger();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            _rawTransaction ??= Transaction.ToArray();
            return new Struct(referenceCounter) { BlockIndex, _rawTransaction, (byte)State };
        }
    }
}
