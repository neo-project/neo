using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle
{
    public class OraclePool
    {
        /// <summary>
        /// Service
        /// </summary>
        public readonly OracleService Service;

        /// <summary>
        /// Global queue
        /// </summary>
        private readonly ConcurrentQueue<Transaction> _queue = new ConcurrentQueue<Transaction>();

        /// <summary>
        /// On oracle result
        /// </summary>
        public Action<Transaction, OracleResultsCache> OnResult { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="service">Service</param>
        public OraclePool(OracleService service)
        {
            Service = service;
        }

        /// <summary>
        /// Enqueue HTTP Request
        /// </summary>
        /// <param name="oracleTx">Oracle transaction</param>
        public void EnqueueAsync(Transaction oracleTx)
        {
            _queue.Enqueue(oracleTx);
        }

        /// <summary>
        /// Start thread
        /// </summary>
        /// <param name="cancellationToken">Cancel token</param>
        public Task StartThread(CancellationToken cancellationToken)
        {
            var task = new Task(() => ConsumeQueue(cancellationToken), cancellationToken);
            task.Start();
            return task;
        }

        /// <summary>
        /// Consume queue
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private void ConsumeQueue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var tx))
                {
                    var res = Service.Process(tx);
                    OnResult?.Invoke(tx, res);
                }

                Thread.Sleep(1);
            }
        }
    }
}
