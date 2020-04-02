using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols.Https;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle
{
    public class OracleService : UntypedActor
    {
        /// <summary>
        /// A flag for know if the pool was ordered or not
        /// </summary>
        private bool _itsDirty = false;
        private long _isStarted = 0;
        private CancellationTokenSource _cancel;
        private readonly IActorRef _localNode;
        private readonly Wallet _wallet;

        /// <summary>
        /// Number of threads for processing the oracle
        /// </summary>
        private readonly Task[] _oracleTasks = new Task[4]; // TODO: Set this
        /// <summary>
        /// _oracleTasks will consume from this pool
        /// </summary>
        private readonly BlockingCollection<Transaction> _asyncPool = new BlockingCollection<Transaction>();
        /// <summary>
        /// Sortable pool
        /// </summary>
        private readonly List<Transaction> _orderedPool = new List<Transaction>();
        /// <summary>
        /// HTTPS protocol
        /// </summary>
        private static readonly OracleHttpsProtocol _https = new OracleHttpsProtocol();

        /// <summary>
        /// Akka message
        /// </summary>
        public class OracleServiceResponse
        {
            public OracleExecutionCache ExecutionResult;
            public byte[] OracleSignature;
        }

        /// <summary>
        /// Oracle
        /// </summary>
        public Func<OracleRequest, OracleResponse> Oracle { get; }

        /// <summary>
        /// Is started
        /// </summary>
        public bool IsStarted => Interlocked.Read(ref _isStarted) == 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localNode">Local node</param>
        /// <param name="wallet">Wallet</param>
        public OracleService(IActorRef localNode, Wallet wallet)
        {
            Oracle = Process;
            _localNode = localNode;
            _wallet = wallet;
        }

        /// <summary>
        /// Can receive TX
        /// </summary>
        /// <param name="message"></param>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Transaction tx:
                    {
                        // TODO: Check that it's a oracleTx
                        // TODO: Should we check the max, or use MemoryPool?

                        lock (_orderedPool)
                        {
                            // Add and sort

                            _orderedPool.Add(tx);
                            _itsDirty = true;

                            // Pop one item if it's needed

                            if (_asyncPool.Count <= 0)
                            {
                                PopTransaction();
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Move one transaction from _orderedPool to _asyncPool
        /// </summary>
        private void PopTransaction()
        {
            if (_orderedPool.Count > 0)
            {
                if (_itsDirty)
                {
                    // It will require a sort before pop the item

                    _orderedPool.Sort((a, b) => b.FeePerByte.CompareTo(a.FeePerByte));
                    _itsDirty = false;
                }

                var entry = _orderedPool[0];
                _orderedPool.RemoveAt(0);

                _asyncPool.Add(entry);
            }
        }

        /// <summary>
        /// Start oracle
        /// </summary>
        public void Start()
        {
            if (Interlocked.Exchange(ref _isStarted, 1) == 0) return;

            // Create tasks

            _cancel = new CancellationTokenSource();

            for (int x = 0; x < _oracleTasks.Length; x++)
            {
                _oracleTasks[x] = new Task(() =>
                {
                    // TODO: it sould be sorted by fee

                    foreach (var tx in _asyncPool.GetConsumingEnumerable(_cancel.Token))
                    {
                        ProcessTransaction(tx);

                        lock (_orderedPool)
                        {
                            PopTransaction();
                        }
                    }
                },
                _cancel.Token);
            }

            // Start tasks

            foreach (var task in _oracleTasks) task.Start();
        }

        /// <summary>
        /// Stop oracle
        /// </summary>
        public void Stop()
        {
            if (Interlocked.Exchange(ref _isStarted, 0) == 1) return;

            _cancel.Cancel();

            for (int x = 0; x < _oracleTasks.Length; x++)
            {
                _oracleTasks[x].Wait();
                _oracleTasks[x].Dispose();
                _oracleTasks[x] = null;
            }

            _cancel.Dispose();
            _cancel = null;

            // Clean queue

            _orderedPool.Clear();

            while (_asyncPool.Count > 0)
            {
                _asyncPool.TryTake(out _);
            }
        }

        /// <summary>
        /// Process transaction
        /// </summary>
        /// <param name="tx">Transaction</param>
        private void ProcessTransaction(Transaction tx)
        {
            var oracle = new OracleExecutionCache(Process);
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            using var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, false, oracle);

            if (engine.Execute() == VM.VMState.HALT)
            {
                // Send oracle result

                _localNode.Tell(new OracleServiceResponse()
                {
                    ExecutionResult = oracle,
                    OracleSignature = Sign(oracle)
                });
            }
            else
            {
                // TODO: Send something or not
            }
        }

        /// <summary>
        /// Sign
        /// </summary>
        /// <param name="oracle">Oracle result</param>
        /// <returns>Signature</returns>
        private byte[] Sign(OracleExecutionCache oracle)
        {
            // TODO: With tx or not?

            //using (ScriptBuilder script = new ScriptBuilder())
            //{
            //    // Compose script
            //    var tx = _wallet.MakeTransaction(script.ToArray(), null, null, null);
            //    return tx.Witnesses[0];
            //}

            return new byte[0];
        }

        #region Public Static methods

        /// <summary>
        /// Process oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Return Oracle response</returns>
        public static OracleResponse Process(OracleRequest request)
        {
            return request switch
            {
                OracleHttpsRequest https => _https.Process(https),
                _ => OracleResponse.CreateError(request.Hash, OracleResultError.ProtocolError),
            };
        }

        /// <summary>
        /// Filter response
        /// </summary>
        /// <param name="input">Input</param>
        /// <param name="filter">Filter</param>
        /// <param name="result">Result</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterResponse(string input, string filter, out string result)
        {
            if (string.IsNullOrEmpty(filter))
            {
                result = input;
                return true;
            }

            if (FilterResponse(Encoding.UTF8.GetBytes(input), filter, out var bufferResult))
            {
                result = Encoding.UTF8.GetString(bufferResult);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Filter response
        /// </summary>
        /// <param name="input">Input</param>
        /// <param name="filter">Filter</param>
        /// <param name="result">Result</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterResponse(byte[] input, string filter, out byte[] result)
        {
            // TODO: Filter
            //result = "";
            //return false;

            result = input;
            return true;
        }

        #endregion
    }
}
