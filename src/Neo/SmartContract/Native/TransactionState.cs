// Copyright (C) 2015-2025 The Neo Project.
//
// TransactionState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
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
        public uint BlockIndex { get; set; }

        /// <summary>
        /// The transaction, if the transaction is trimmed this value will be null
        /// </summary>
        public Transaction? Transaction
        {
            get => _transaction;
            set
            {
                // Clean raw transaction
                _rawTransaction = ReadOnlyMemory<byte>.Empty;
                _transaction = value;
            }
        }

        /// <summary>
        /// The execution state
        /// </summary>
        public VMState State { get; set; }

        private Transaction? _transaction;
        private ReadOnlyMemory<byte> _rawTransaction = ReadOnlyMemory<byte>.Empty;

        void IInteroperable.FromReplica(IInteroperable replica)
        {
            var from = (TransactionState)replica;
            BlockIndex = from.BlockIndex;
            _transaction = from.Transaction;
            State = from.State;
            _rawTransaction = from._rawTransaction;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            var @struct = (Struct)stackItem;
            BlockIndex = (uint)@struct[0].GetInteger();

            // Conflict record.
            if (@struct.Count == 1) return;

            // Fully-qualified transaction.
            _rawTransaction = ((ByteString)@struct[1]).Memory;
            _transaction = _rawTransaction.AsSerializable<Transaction>();
            State = (VMState)(byte)@struct[2].GetInteger();
        }

        StackItem IInteroperable.ToStackItem(IReferenceCounter referenceCounter)
        {
            if (Transaction is null)
                return new Struct(referenceCounter) { BlockIndex };
            if (_rawTransaction.IsEmpty)
                _rawTransaction = Transaction.ToArray();
            return new Struct(referenceCounter) { BlockIndex, _rawTransaction, (byte)State };
        }
    }
}

#nullable disable
