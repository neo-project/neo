using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols.Https;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.Wallets;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle
{
    public class OracleService : UntypedActor
    {
        private long _isStarted = 0;
        private CancellationTokenSource _cancel;
        private readonly IActorRef _localNode;
        private readonly Wallet _wallet;

        private readonly Task[] _oracleTasks = new Task[4];
        private readonly BlockingCollection<Transaction> _pool = new BlockingCollection<Transaction>();

        private static readonly OracleHTTPProtocol _https = new OracleHTTPProtocol();

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
        public Func<OracleRequest, OracleResult> Oracle { get; }

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

                        _pool.Add(tx);
                        break;
                    }
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
                    foreach (var tx in _pool.GetConsumingEnumerable(_cancel.Token))
                    {
                        ProcessTransaction(tx);
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

            while (_pool.Count > 0)
            {
                _pool.TryTake(out _);
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

                var msg = new OracleServiceResponse()
                {
                    ExecutionResult = oracle,
                    OracleSignature = Sign(oracle)
                };

                _localNode.Tell(oracle);
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

        /// <summary>
        /// Process oracle request
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Return Oracle response</returns>
        public static OracleResult Process(OracleRequest request)
        {
            switch (request)
            {
                case OracleHttpsRequest https:
                    {
                        short seconds;

                        using (var snapshot = Blockchain.Singleton.GetSnapshot())
                        {
                            seconds = (short)NativeContract.Oracle.GetConfig(snapshot, HttpConfig.Timeout).ToBigInteger();
                        }

                        return _https.Process(https, TimeSpan.FromSeconds(seconds));
                    }
            }

            return OracleResult.CreateError(UInt256.Zero, request.Hash, OracleResultError.ProtocolError);
        }
    }
}
