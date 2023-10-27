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
using System.Linq;

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
        /// The transaction, if the transaction is trimmed this value will be null
        /// </summary>
        public Transaction Transaction;

        public UInt160[] ConflictingSigners;

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
                ConflictingSigners = ConflictingSigners,
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
            ConflictingSigners = from.ConflictingSigners;
            State = from.State;
            NotificationMerkleRoot = from.NotificationMerkleRoot;
            if (_rawTransaction.IsEmpty)
                _rawTransaction = from._rawTransaction;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            if (@struct.Count == 1)
            {
                ConflictingSigners = ((VM.Types.Array)@struct[0]).Select(u => new UInt160(u.GetSpan())).ToArray();
                return;
            }
            BlockIndex = (uint)@struct[0].GetInteger();
            _rawTransaction = ((ByteString)@struct[1]).Memory;
            Transaction = _rawTransaction.AsSerializable<Transaction>();
            State = (VMState)(byte)@struct[2].GetInteger();
            NotificationMerkleRoot = new UInt256(@struct[3].GetSpan());
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            if (Transaction is null) return new Struct(referenceCounter) { new VM.Types.Array(referenceCounter, ConflictingSigners.Select(u => new ByteString(u.ToArray())).ToArray()) };
            if (_rawTransaction.IsEmpty)
                _rawTransaction = Transaction.ToArray();
            return new Struct(referenceCounter) { BlockIndex, _rawTransaction, (byte)State, NotificationMerkleRoot.ToArray() };
        }
    }
}
