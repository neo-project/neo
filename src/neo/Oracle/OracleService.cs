using Akka.Actor;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols.Https;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
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
        private bool _isDirty = false;
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

        #region Protocols

        /// <summary>
        /// HTTPS protocol
        /// </summary>
        internal static OracleHttpsProtocol HTTPSProtocol { get; } = new OracleHttpsProtocol();

        #endregion

        /// <summary>
        /// Akka message
        /// </summary>
        public class OracleServiceResponse
        {
            public OracleExecutionCache ExecutionResult;

            public UInt256 UserTxHash;
            public byte[] OracleResponseScript;
            public byte[] OracleResponseSignature;
        }

        /// <summary>
        /// Oracle
        /// </summary>
        public Func<OracleRequest, OracleResponse> Oracle { get; }

        /// <summary>
        /// Is started
        /// </summary>
        public bool IsStarted => Interlocked.Read(ref _isStarted) == 1;

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
                            _isDirty = true;

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
                if (_isDirty && _orderedPool.Count > 1)
                {
                    // It will require a sort before pop the item

                    _orderedPool.Sort((a, b) => b.FeePerByte.CompareTo(a.FeePerByte));
                    _isDirty = false;
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
            if (Interlocked.Exchange(ref _isStarted, 1) != 0) return;

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
            if (Interlocked.Exchange(ref _isStarted, 0) != 1) return;

            _cancel.Cancel();

            for (int x = 0; x < _oracleTasks.Length; x++)
            {
                try { _oracleTasks[x].Wait(); } catch { }
                try { _oracleTasks[x].Dispose(); } catch { }
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
            engine.LoadScript(tx.Script);

            if (engine.Execute() == VMState.HALT)
            {
                // Send oracle result

                var responseTx = CreateResponseTransaction(oracle, tx);
                Sign(responseTx, out var signature);

                var response = new OracleServiceResponse()
                {
                    ExecutionResult = oracle,
                    UserTxHash = tx.Hash,
                    OracleResponseScript = responseTx.Script,
                    OracleResponseSignature = signature
                };

                _localNode.Tell(response);
            }
            else
            {
                // TODO: Send something or not
            }
        }

        /// <summary>
        /// Create Oracle response transaction
        /// We need to create a deterministic TX for this result/oracleRequest
        /// </summary>
        /// <param name="oracle">Oracle</param>
        /// <param name="requestTx">Request Hash</param>
        /// <returns>Transaction</returns>
        private Transaction CreateResponseTransaction(OracleExecutionCache oracle, Transaction requestTx)
        {
            using ScriptBuilder script = new ScriptBuilder();

            script.EmitAppCall(NativeContract.Oracle.Hash, "setOracleResponse", requestTx.Hash, oracle.ToArray());

            return new Transaction()
            {
                Attributes = new TransactionAttribute[]
                {
                    // DependsOn = hash
                },
                Cosigners = new Cosigner[0],
                NetworkFee = 1_000_000, // TODO: Define fee
                SystemFee = 1_000_000,
                Sender = UInt160.Zero, // <- OracleSender
                Nonce = requestTx.Nonce,
                ValidUntilBlock = requestTx.ValidUntilBlock,
                Witnesses = new Witness[0],
                Script = script.ToArray()
            };
        }

        /// <summary>
        /// Sign
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <returns>Signature</returns>
        private void Sign(Transaction tx, out byte[] signature)
        {
            // Sign the transaction and extract the signature of this oracle

            signature = new byte[0];
        }

        #region Public Static methods

        /// <summary>
        /// Process oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Return Oracle response</returns>
        public static OracleResponse Process(OracleRequest request)
        {
            try
            {
                return request switch
                {
                    OracleHttpsRequest https => HTTPSProtocol.Process(https),
                    _ => OracleResponse.CreateError(request.Hash, OracleResultError.ProtocolError),
                };
            }
            catch
            {
                return OracleResponse.CreateError(request.Hash, OracleResultError.ServerError);
            }
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
