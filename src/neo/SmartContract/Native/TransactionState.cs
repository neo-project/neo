// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents a transaction that has been included in a block.
    /// </summary>
    public class TransactionState : IInteroperable
    {
        /// <summary>
        /// The transaction is trimmed.
        /// To indicate this state is a placeholder for a conflict transaction.
        /// </summary>
        public bool Trimmed;

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

        private ReadOnlyMemory<byte> _rawTransaction;

        IInteroperable IInteroperable.Clone()
        {
            return new TransactionState
            {
                BlockIndex = BlockIndex,
                Transaction = Transaction,
                State = State,
                _rawTransaction = _rawTransaction
            };
        }

        void IInteroperable.FromReplica(IInteroperable replica)
        {
            TransactionState from = (TransactionState)replica;
            BlockIndex = from.BlockIndex;
            Transaction = from.Transaction;
            State = from.State;
            if (_rawTransaction.IsEmpty)
                _rawTransaction = from._rawTransaction;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Trimmed = @struct[0].GetBoolean();
            if (Trimmed) return;
            BlockIndex = (uint)@struct[1].GetInteger();
            _rawTransaction = ((ByteString)@struct[2]).Memory;
            Transaction = _rawTransaction.AsSerializable<Transaction>();
            State = (VMState)(byte)@struct[3].GetInteger();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            if (Trimmed) return new Struct(referenceCounter) { true };
            if (_rawTransaction.IsEmpty)
                _rawTransaction = Transaction.ToArray();
            return new Struct(referenceCounter) { false, BlockIndex, _rawTransaction, (byte)State };
        }
    }
}
