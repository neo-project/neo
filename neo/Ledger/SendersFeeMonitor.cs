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
            bool recorded = _senderFee.TryGetValue(sender, out var value);
            _senderFeeRwLock.ExitReadLock();
            if (recorded)
                return value;
            else
                return BigInteger.Zero;
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

        public void RemoveSenderFee(Transaction tx)
        {
            _senderFeeRwLock.EnterWriteLock();
            _senderFee[tx.Sender] -= tx.SystemFee + tx.NetworkFee;
            if (_senderFee[tx.Sender] == 0) _senderFee.Remove(tx.Sender);
            _senderFeeRwLock.ExitWriteLock();
        }

        public void ClearSenderFee()
        {
            _senderFeeRwLock.EnterWriteLock();
            _senderFee.Clear();
            _senderFeeRwLock.ExitWriteLock();
        }
    }
}
