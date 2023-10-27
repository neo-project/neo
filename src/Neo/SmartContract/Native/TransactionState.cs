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

        /// <summary>
        /// The merkle root of the notifications.
        /// </summary>
        public UInt256 NotificationMerkleRoot;

        private ReadOnlyMemory<byte> _rawTransaction;

        IInteroperable IInteroperable.Clone()
        {
            return new TransactionState
            {
                BlockIndex = BlockIndex,
                Transaction = Transaction,
                State = State,
                NotificationMerkleRoot = NotificationMerkleRoot,
                _rawTransaction = _rawTransaction
            };
        }

        void IInteroperable.FromReplica(IInteroperable replica)
        {
            TransactionState from = (TransactionState)replica;
            BlockIndex = from.BlockIndex;
            Transaction = from.Transaction;
            State = from.State;
            NotificationMerkleRoot = from.NotificationMerkleRoot;
            if (_rawTransaction.IsEmpty)
                _rawTransaction = from._rawTransaction;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            BlockIndex = (uint)@struct[0].GetInteger();
            _rawTransaction = ((ByteString)@struct[1]).Memory;
            Transaction = _rawTransaction.AsSerializable<Transaction>();
            State = (VMState)(byte)@struct[2].GetInteger();
            NotificationMerkleRoot = new UInt256(@struct[3].GetSpan());
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            if (_rawTransaction.IsEmpty)
                _rawTransaction = Transaction.ToArray();
            return new Struct(referenceCounter) { BlockIndex, _rawTransaction, (byte)State, NotificationMerkleRoot.ToArray() };
        }
    }
}
