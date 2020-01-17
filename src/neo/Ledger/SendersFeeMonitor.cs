using Neo.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Neo.Ledger
{
    public class SendersFeeMonitor
    {
        private readonly ReaderWriterLockSlim _senderFeeRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the memory pool.
        /// </summary>
        private readonly Dictionary<UInt160, BigInteger> _senderFee = new Dictionary<UInt160, BigInteger>();

        public BigInteger GetSenderFee(UInt160 sender)
        {
            _senderFeeRwLock.EnterReadLock();
            if (!_senderFee.TryGetValue(sender, out var value))
                value = BigInteger.Zero;
            _senderFeeRwLock.ExitReadLock();
            return value;
        }

        public void AddSenderFee(Transaction tx)
        {
            _senderFeeRwLock.EnterWriteLock();
            if (_senderFee.TryGetValue(tx.Sender, out var value))
                _senderFee[tx.Sender] = value + tx.SystemFee + tx.NetworkFee;
            else
                _senderFee.Add(tx.Sender, tx.SystemFee + tx.NetworkFee);
            _senderFeeRwLock.ExitWriteLock();
        }

        public bool TryAddSenderFee(Transaction tx, BigInteger balance)
        {
            bool ret = true;
            BigInteger sum = tx.SystemFee + tx.NetworkFee;

            _senderFeeRwLock.EnterWriteLock();
            if (_senderFee.TryGetValue(tx.Sender, out var value))
            {
                sum += value;
                if (balance < sum) ret = false;
                else _senderFee[tx.Sender] = sum;
            }
            else
            {
                if (balance < sum) ret = false;
                else _senderFee.Add(tx.Sender, sum);
            }
            _senderFeeRwLock.ExitWriteLock();

            return ret;
        }

        public void RemoveSenderFee(Transaction tx)
        {
            _senderFeeRwLock.EnterWriteLock();
            if ((_senderFee[tx.Sender] -= tx.SystemFee + tx.NetworkFee) == 0) _senderFee.Remove(tx.Sender);
            _senderFeeRwLock.ExitWriteLock();
        }
    }
}
