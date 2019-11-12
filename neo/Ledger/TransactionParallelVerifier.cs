using Akka.Actor;
using Akka.Event;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Collections.Concurrent;
using System.Threading;

namespace Neo.Ledger
{
    public class TransactionParallelVerifier : UntypedActor
    {
        private Snapshot _currentSnapshot;
        private long feePeerByte;
        private readonly MemoryPool Mempool;
        public readonly int Index;

        /// <summary>
        /// This lock is to avoid concurrent issue when CurrentSnapshot is updating along with CheckWitnesses is being called.
        /// </summary>
        private readonly ReaderWriterLockSlim _updateSnapshotLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public Snapshot CurrentSnapshot
        {
            get { return _currentSnapshot; }
            set
            {
                _updateSnapshotLock.EnterWriteLock();
                try
                {
                    _currentSnapshot = value;
                    feePeerByte = NativeContract.Policy.GetFeePerByte(value);
                }
                finally
                {
                    _updateSnapshotLock.ExitWriteLock();
                }
            }
        }

        public TransactionParallelVerifier(ConcurrentDictionary<int, TransactionParallelVerifier> parallelVerifierDic, Snapshot snapshot, MemoryPool pool, int index)
        {
            this.CurrentSnapshot = snapshot;
            this.Mempool = pool;
            this.Index = index;
            parallelVerifierDic.TryAdd(index, this);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case ParallelVerifyTransaction parallelVerifiedTransaction:
                    OnParallelVerifiedTransactionReceived(parallelVerifiedTransaction);
                    break;
            }
        }

        private void OnParallelVerifiedTransactionReceived(ParallelVerifyTransaction parallelVerifiedTransaction)
        {
            if (Verify(parallelVerifiedTransaction.Transaction))
            {
                Context.Parent.Tell(parallelVerifiedTransaction, Sender);
            }
            else
            {
                Mempool.VerifyingSenderFeeMonitor.RemoveSenderVerifyingFee(parallelVerifiedTransaction.Transaction);
                Sender.Tell(RelayResultReason.Invalid);
            }
        }

        private bool Verify(Transaction transaction)
        {
            int size = transaction.Size;
            if (size > Transaction.MaxTransactionSize) return false;
            
            _updateSnapshotLock.EnterReadLock();
            long net_fee = transaction.NetworkFee - size * feePeerByte;
            if (net_fee < 0) return false;
            try
            {
                return transaction.VerifyWitnesses(CurrentSnapshot, net_fee);
            }
            finally
            {
                _updateSnapshotLock.ExitReadLock();
            }
        }

        public static Props Props(ConcurrentDictionary<int, TransactionParallelVerifier> parallelVerifierDic, Snapshot snapshot, MemoryPool mempool, int index) => Akka.Actor.Props.Create(() => new TransactionParallelVerifier(parallelVerifierDic, snapshot, mempool, index));
    }
}
