// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Ledger
{
    /// <summary>
    /// The context used to verify the transaction.
    /// </summary>
    public class TransactionVerificationContext
    {
        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the memory pool.
        /// </summary>
        private readonly Dictionary<UInt160, BigInteger> senderFee = new();

        /// <summary>
        /// Store oracle responses
        /// </summary>
        private readonly Dictionary<ulong, UInt256> oracleResponses = new();

        /// <summary>
        /// Adds a verified <see cref="Transaction"/> to the context.
        /// </summary>
        /// <param name="tx">The verified <see cref="Transaction"/>.</param>
        public void AddTransaction(Transaction tx)
        {
            var oracle = tx.GetAttribute<OracleResponse>();
            if (oracle != null) oracleResponses.Add(oracle.Id, tx.Hash);

            if (senderFee.TryGetValue(tx.Sender, out var value))
                senderFee[tx.Sender] = value + tx.SystemFee + tx.NetworkFee;
            else
                senderFee.Add(tx.Sender, tx.SystemFee + tx.NetworkFee);
        }

        /// <summary>
        /// Determine whether the specified <see cref="Transaction"/> conflicts with other transactions.
        /// </summary>
        /// <param name="tx">The specified <see cref="Transaction"/>.</param>
        /// <param name="snapshot">The snapshot used to verify the <see cref="Transaction"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="Transaction"/> passes the check; otherwise, <see langword="false"/>.</returns>
        public bool CheckTransaction(Transaction tx, DataCache snapshot)
        {
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, tx.Sender);
            senderFee.TryGetValue(tx.Sender, out var totalSenderFeeFromPool);

            BigInteger fee = tx.SystemFee + tx.NetworkFee + totalSenderFeeFromPool;
            if (balance < fee) return false;

            var oracle = tx.GetAttribute<OracleResponse>();
            if (oracle != null && oracleResponses.ContainsKey(oracle.Id))
                return false;

            return true;
        }

        /// <summary>
        /// Removes a <see cref="Transaction"/> from the context.
        /// </summary>
        /// <param name="tx">The <see cref="Transaction"/> to be removed.</param>
        public void RemoveTransaction(Transaction tx)
        {
            if ((senderFee[tx.Sender] -= tx.SystemFee + tx.NetworkFee) == 0) senderFee.Remove(tx.Sender);

            var oracle = tx.GetAttribute<OracleResponse>();
            if (oracle != null) oracleResponses.Remove(oracle.Id);
        }
    }
}
