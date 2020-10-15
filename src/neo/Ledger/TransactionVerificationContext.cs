using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Ledger
{
    public class TransactionVerificationContext
    {
        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the memory pool.
        /// </summary>
        private readonly Dictionary<UInt160, BigInteger> senderFee = new Dictionary<UInt160, BigInteger>();

        public void AddTransaction(Transaction tx)
        {
            if (senderFee.TryGetValue(tx.Sender, out var value))
                senderFee[tx.Sender] = value + tx.SystemFee + tx.NetworkFee;
            else
                senderFee.Add(tx.Sender, tx.SystemFee + tx.NetworkFee);
        }

        public bool CheckTransaction(Transaction tx, StoreView snapshot)
        {
            BigInteger balance = NativeContract.GAS.BalanceOf(snapshot, tx.Sender);
            senderFee.TryGetValue(tx.Sender, out var totalSenderFeeFromPool);
            BigInteger fee = tx.SystemFee + tx.NetworkFee + totalSenderFeeFromPool;
            if (balance >= fee) return true;

            BigInteger pending = NativeContract.NEO.UnclaimedGas(snapshot, tx.Sender, snapshot.Height);
            return balance + pending >= fee;
        }

        public void RemoveTransaction(Transaction tx)
        {
            if ((senderFee[tx.Sender] -= tx.SystemFee + tx.NetworkFee) == 0) senderFee.Remove(tx.Sender);
        }
    }
}
