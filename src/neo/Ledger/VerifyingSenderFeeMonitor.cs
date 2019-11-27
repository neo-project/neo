using Neo.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Neo.Ledger
{
    public class VerifyingSenderFeeMonitor
    {
        /// <summary>
        /// Stores the frozen fee of transactions for each account during Blockchain's trasaction verification.
        /// </summary>
        private readonly Dictionary<UInt160, BigInteger> _senderVerifyingFee = new Dictionary<UInt160, BigInteger>();

        /// <summary>
        /// We need a readwrite lock for parallel updating _senderVerifyFrozenFee.
        /// </summary>
        private readonly ReaderWriterLockSlim _verifyingFeeRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public BigInteger GetSenderVerifyingFee(UInt160 sender)
        {
            _verifyingFeeRwLock.EnterReadLock();
            try
            {
                if (!_senderVerifyingFee.TryGetValue(sender, out BigInteger value))
                    return BigInteger.Zero;
                return value;
            }
            finally
            {
                _verifyingFeeRwLock.ExitReadLock();
            }
        }

        public void AddSenderVerifyingFee(Transaction tx)
        {
            _verifyingFeeRwLock.EnterWriteLock();
            try
            {
                if (!_senderVerifyingFee.TryGetValue(tx.Sender, out BigInteger value))
                    _senderVerifyingFee.Add(tx.Sender, tx.SystemFee + tx.NetworkFee);
                else
                    _senderVerifyingFee[tx.Sender] = value + tx.SystemFee + tx.NetworkFee;
            }
            finally
            {
                _verifyingFeeRwLock.ExitWriteLock();
            }
        }

        public void RemoveSenderVerifyingFee(Transaction tx)
        {
            _verifyingFeeRwLock.EnterWriteLock();
            try
            {
                if ((_senderVerifyingFee[tx.Sender] -= tx.SystemFee + tx.NetworkFee) == 0) _senderVerifyingFee.Remove(tx.Sender);
            }
            finally
            {
                _verifyingFeeRwLock.ExitWriteLock();
            }
        }
    }
}
