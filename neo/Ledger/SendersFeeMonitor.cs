using Neo.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Neo.Ledger
{
    public class SendersFeeMonitor
    {
        private static readonly ReaderWriterLockSlim _memPoolSenderFeeRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private static readonly ReaderWriterLockSlim _consensusSenderFeeRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the memory pool.
        /// </summary>
        private static readonly Dictionary<UInt160, BigInteger> _memPoolSenderFee = new Dictionary<UInt160, BigInteger>();

        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the consensus context.
        /// </summary>
        private static readonly Dictionary<UInt160, BigInteger> _consensusSenderFee = new Dictionary<UInt160, BigInteger>();

        public static BigInteger GetMemPoolSenderFee(UInt160 sender)
        {
            _memPoolSenderFeeRwLock.EnterReadLock();
            bool recorded = _memPoolSenderFee.TryGetValue(sender, out var value);
            _memPoolSenderFeeRwLock.ExitReadLock();
            if (recorded)
                return value;
            else
                return BigInteger.Zero;
        }

        public static void AddMemPoolSenderFee(Transaction tx)
        {
            _memPoolSenderFeeRwLock.EnterWriteLock();
            if (_memPoolSenderFee.TryGetValue(tx.Sender, out var value))
                _memPoolSenderFee[tx.Sender] = value + tx.SystemFee + tx.NetworkFee;
            else
                _memPoolSenderFee.Add(tx.Sender, tx.SystemFee + tx.NetworkFee);
            _memPoolSenderFeeRwLock.ExitWriteLock();
        }

        public static void RemoveMemPoolSenderFee(Transaction tx)
        {
            _memPoolSenderFeeRwLock.EnterWriteLock();
            _memPoolSenderFee[tx.Sender] -= tx.SystemFee + tx.NetworkFee;
            if (_memPoolSenderFee[tx.Sender] == 0) _memPoolSenderFee.Remove(tx.Sender);
            _memPoolSenderFeeRwLock.ExitWriteLock();
        }

        public static void ClearMemPoolSenderFee()
        {
            _memPoolSenderFeeRwLock.EnterWriteLock();
            _memPoolSenderFee.Clear();
            _memPoolSenderFeeRwLock.ExitWriteLock();
        }

        public static BigInteger GetConsensusSenderFee(UInt160 sender)
        {
            _consensusSenderFeeRwLock.EnterReadLock();
            bool recorded = _consensusSenderFee.TryGetValue(sender, out var value);
            _consensusSenderFeeRwLock.ExitReadLock();
            if (recorded)
                return value;
            else
                return BigInteger.Zero;
        }

        public static void AddConsensusSenderFee(Transaction tx)
        {
            _consensusSenderFeeRwLock.EnterWriteLock();
            if (_consensusSenderFee.TryGetValue(tx.Sender, out var value))
                _consensusSenderFee[tx.Sender] = value + tx.SystemFee + tx.NetworkFee;
            else
                _consensusSenderFee.Add(tx.Sender, tx.SystemFee + tx.NetworkFee);
            _consensusSenderFeeRwLock.ExitWriteLock();
        }

        public static void RemoveConsensusSenderFee(Transaction tx)
        {
            _consensusSenderFeeRwLock.EnterWriteLock();
            _consensusSenderFee[tx.Sender] -= tx.SystemFee + tx.NetworkFee;
            if (_consensusSenderFee[tx.Sender] == 0) _consensusSenderFee.Remove(tx.Sender);
            _consensusSenderFeeRwLock.ExitWriteLock();
        }

        public static void ClearConsensusSenderFee()
        {
            _consensusSenderFeeRwLock.EnterWriteLock();
            _consensusSenderFee.Clear();
            _consensusSenderFeeRwLock.ExitWriteLock();
        }
    }
}
