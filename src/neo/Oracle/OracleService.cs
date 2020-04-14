using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols.Https;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle
{
    public class OracleService : UntypedActor
    {
        #region Sub classes

        internal class StartMessage { }

        private class RequestItem : PoolItem
        {
            // Request

            public readonly Transaction RequestTransaction;

            // Response

            public readonly Contract Contract;
            public Transaction ResponseTransaction;
            private ContractParametersContext ResponseContext;

            public bool IsCompleted => ResponseContext?.Completed == true;

            internal RequestItem(Contract contract, Transaction requestTx) : base(requestTx)
            {
                Contract = contract;
                RequestTransaction = requestTx;
            }

            public bool AddSinature(ResponseItem response)
            {
                if (response.Data.TransactionRequestHash != RequestTransaction.Hash)
                {
                    return false;
                }

                if (ResponseTransaction == null)
                {
                    if (response.ResponseTx == null)
                    {
                        return false;
                    }

                    // Oracle service could attach the real TX

                    ResponseTransaction = response.ResponseTx;
                    ResponseContext = new ContractParametersContext(response.ResponseTx);
                }

                // TODO: Check duplicate call

                return ResponseContext.AddSignature(Contract, response.OraclePub, response.Data.Signature) == true;
            }
        }

        private class ResponseCollection : IEnumerable<ResponseItem>
        {
            public readonly DateTime Timestamp;
            public readonly IDictionary<UInt160, ResponseItem> Items = new ConcurrentDictionary<UInt160, ResponseItem>();

            public ResponseCollection(ResponseItem item)
            {
                Timestamp = item.Timestamp;
                Add(item);
            }

            public bool Add(ResponseItem item)
            {
                return Items.TryAdd(item.Hash, item);
            }

            public IEnumerator<ResponseItem> GetEnumerator()
            {
                return (IEnumerator<ResponseItem>)Items.Values.ToArray().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class ResponseItem : PoolItem
        {
            public readonly OraclePayload Msg;
            public readonly OracleResponseSignature Data;
            public readonly Transaction ResponseTx;

            public ECPoint OraclePub => Msg.OraclePub;
            public UInt160 Hash => Msg.Hash;

            internal ResponseItem(OraclePayload payload, Transaction responseTx) : base(responseTx)
            {
                Msg = payload;
                Data = payload.OracleSignature;
                ResponseTx = responseTx;
            }
        }

        #endregion

        private const long MaxGasFilter = 1_000_000;

        private long _isStarted = 0;
        private Contract _lastContract;
        private readonly IActorRef _localNode;
        private (Contract Contract, KeyPair Key)[] _accounts;
        private CancellationTokenSource _cancel;

        /// <summary>
        /// Number of threads for processing the oracle
        /// </summary>
        private readonly Task[] _oracleTasks = new Task[4]; // TODO: Set this

        /// <summary>
        /// _oracleTasks will consume from this pool
        /// </summary>
        private readonly BlockingCollection<Transaction> _asyncPool = new BlockingCollection<Transaction>();

        /// <summary>
        /// Queue
        /// </summary>
        private readonly SortedConcurrentDictionary<UInt256, Transaction> _queue;

        #region Protocols

        /// <summary>
        /// HTTPS protocol
        /// </summary>
        internal static OracleHttpsProtocol HTTPSProtocol { get; } = new OracleHttpsProtocol();

        #endregion

        /// <summary>
        /// Oracle
        /// </summary>
        public Func<OracleRequest, OracleResponse> Oracle { get; }

        /// <summary>
        /// Pending user Transactions
        /// </summary>
        private readonly SortedConcurrentDictionary<UInt256, RequestItem> _pendingOracleRequest;

        /// <summary>
        /// Pending oracle response Transactions
        /// </summary>
        private readonly SortedConcurrentDictionary<UInt256, ResponseCollection> _pendingOracleResponses;

        /// <summary>
        /// Total maximum capacity of transactions the pool can hold.
        /// </summary>
        public int PendingCapacity { get; }

        /// <summary>
        /// MemoryPool
        /// </summary>
        public MemoryPool MemPool { get; }

        /// <summary>
        /// Total requests in the pool.
        /// </summary>
        public int PendingRequestCount => _pendingOracleRequest.Count;

        /// <summary>
        /// Total responses in the pool.
        /// </summary>
        public int PendingResponseCount => _pendingOracleResponses.Count;

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
            MemPool = Blockchain.Singleton.MemPool;
            PendingCapacity = MemPool.Capacity;
            _localNode = localNode;

            // Find oracle account

            using var snapshot = GetSnapshot();
            var oracles = NativeContract.Oracle.GetOracleValidators(snapshot)
                .Select(u => Contract.CreateSignatureRedeemScript(u).ToScriptHash());

            _accounts = wallet?.GetAccounts()
                .Where(u => u.HasKey && !u.Lock && oracles.Contains(u.ScriptHash))
                .Select(u => (u.Contract, u.GetKey()))
                .ToArray();

            if (_accounts.Length == 0)
            {
                throw new ArgumentException("The wallet doesn't have any of the expected accounts");
            }

            _queue = new SortedConcurrentDictionary<UInt256, Transaction>
                (
                Comparer<KeyValuePair<UInt256, Transaction>>.Create(SortEnqueuedRequest), PendingCapacity
                );

            // Create internal collections for pending request/responses

            _pendingOracleRequest = new SortedConcurrentDictionary<UInt256, RequestItem>
                (
                Comparer<KeyValuePair<UInt256, RequestItem>>.Create(SortRequest), PendingCapacity
                );
            _pendingOracleResponses = new SortedConcurrentDictionary<UInt256, ResponseCollection>
                (
                Comparer<KeyValuePair<UInt256, ResponseCollection>>.Create(SortResponse), PendingCapacity
                );
        }

        /// <summary>
        /// For UT
        /// </summary>
        internal virtual SnapshotView GetSnapshot()
        {
            return Blockchain.Singleton.GetSnapshot();
        }

        /// <summary>
        /// Receive AKKA Messages
        /// </summary>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartMessage _:
                    {
                        Start();
                        break;
                    }
                case Transaction tx:
                    {
                        using var snapshot = GetSnapshot();
                        var contract = NativeContract.Oracle.GetOracleMultiSigContract(snapshot);

                        switch (tx.Version)
                        {
                            case TransactionVersion.OracleRequest:
                                {
                                    // Check the cached contract

                                    if (_lastContract?.ScriptHash != contract.ScriptHash)
                                    {
                                        // Reduce the memory load using the same Contract class

                                        _lastContract = contract;
                                    }

                                    // If it's an OracleRequest and it's new, tell it to OracleService

                                    if (_pendingOracleRequest.TryAdd(tx.Hash, new RequestItem(contract, tx)))
                                    {
                                        ReverifyPendingResponses(snapshot, tx.Hash);
                                    }

                                    // Process oracle - Pop one item if it's needed

                                    if (_queue.TryAdd(tx.Hash, tx) && _asyncPool.Count <= 0)
                                    {
                                        PopTransaction();
                                    }

                                    break;
                                }
                            case TransactionVersion.OracleResponse:
                                {
                                    var hashes = tx.GetScriptHashesForVerifying(snapshot);

                                    if (hashes.Length != 1 || hashes[0] != contract.ScriptHash)
                                    {
                                        break;
                                    }

                                    // We should receive only this transactions P2P, never from OracleService

                                    if (tx.VerifyWitnesses(snapshot, 200_000_000))
                                    {
                                        ReverifyPendingResponses(snapshot, tx.OracleRequestTx);
                                    }

                                    // TODO: Send it to mempool?

                                    break;
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
            if (_queue.TryPop(out var entry))
            {
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
                    foreach (var tx in _asyncPool.GetConsumingEnumerable(_cancel.Token))
                    {
                        ProcessRequestTransaction(tx);
                        PopTransaction();
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

            _queue.Clear();
            _pendingOracleRequest.Clear();
            _pendingOracleResponses.Clear();

            while (_asyncPool.Count > 0)
            {
                _asyncPool.TryTake(out _);
            }
        }

        /// <summary>
        /// Process request transaction
        /// </summary>
        /// <param name="tx">Transaction</param>
        private void ProcessRequestTransaction(Transaction tx)
        {
            var oracle = new OracleExecutionCache(Process);
            using var snapshot = GetSnapshot();
            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, false, oracle))
            {
                engine.LoadScript(tx.Script);

                if (engine.Execute() != VMState.HALT)
                {
                    // TODO: If the request TX will FAULT?
                    // oracle.Clear();
                }
            }

            // Create deterministic oracle response

            var responseTx = CreateResponseTransaction(snapshot, oracle, tx);

            foreach (var account in _accounts)
            {
                // Sign the transaction

                var signature = responseTx.Sign(account.Key);

                // Create the payload

                var response = new OraclePayload()
                {
                    OraclePub = account.Key.PublicKey,
                    OracleSignature = new OracleResponseSignature()
                    {
                        OracleExecutionCacheHash = oracle.Hash,
                        Signature = signature,
                        TransactionRequestHash = tx.Hash
                    }
                };

                signature = response.Sign(account.Key);

                var signPayload = new ContractParametersContext(response);
                if (signPayload.AddSignature(account.Contract, account.Key.PublicKey, signature) && signPayload.Completed)
                {
                    response.Witness = signPayload.GetWitnesses()[0];

                    if (TryAddOracleResponse(snapshot, response, responseTx))
                    {
                        // Send my signature by P2P

                        _localNode.Tell(response);
                    }
                }
            }
        }

        /// <summary>
        /// Create Oracle response transaction
        /// We need to create a deterministic TX for this result/oracleRequest
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="oracle">Oracle</param>
        /// <param name="requestTx">Request Hash</param>
        /// <returns>Transaction</returns>
        public static Transaction CreateResponseTransaction(StoreView snapshot, OracleExecutionCache oracle, Transaction requestTx)
        {
            var sender = NativeContract.Oracle.GetOracleMultiSigAddress(snapshot);
            using ScriptBuilder script = new ScriptBuilder();

            script.EmitAppCall(NativeContract.Oracle.Hash, "setOracleResponse", requestTx.Hash, IO.Helper.ToArray(oracle));

            return new Transaction()
            {
                Attributes = new TransactionAttribute[0],
                Version = TransactionVersion.OracleResponse,
                Sender = sender,
                Nonce = requestTx.Nonce,
                ValidUntilBlock = requestTx.ValidUntilBlock,
                OracleRequestTx = requestTx.Hash,
                Cosigners = new Cosigner[]
                {
                    new Cosigner()
                    {
                        Account = sender,
                        AllowedContracts = new UInt160[]{ NativeContract.Oracle.Hash },
                        Scopes = WitnessScope.CustomContracts
                    }
                },
                NetworkFee = 1_000_000, // TODO: Define fee
                SystemFee = 1_000_000,  // TODO: Define fee
                Witnesses = new Witness[0],
                Script = script.ToArray()
            };
        }

        #region Sorts

        private int SortRequest(KeyValuePair<UInt256, RequestItem> a, KeyValuePair<UInt256, RequestItem> b)
        {
            return a.Value.CompareTo(b.Value);
        }

        private int SortEnqueuedRequest(KeyValuePair<UInt256, Transaction> a, KeyValuePair<UInt256, Transaction> b)
        {
            return a.Value.FeePerByte.CompareTo(b.Value.FeePerByte);
        }

        private int SortResponse(KeyValuePair<UInt256, ResponseCollection> a, KeyValuePair<UInt256, ResponseCollection> b)
        {
            return a.Value.Timestamp.CompareTo(b.Value.Timestamp);
        }

        #endregion

        /// <summary>
        /// Try add oracle response payload
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="oracle">Oracle</param>
        /// <param name="responseTx">Response TX (from OracleService)</param>
        /// <returns>True if it was added</returns>
        internal bool TryAddOracleResponse(StoreView snapshot, OraclePayload oracle, Transaction responseTx)
        {
            return TryAddOracleResponse(snapshot, new ResponseItem(oracle, responseTx));
        }

        /// <summary>
        /// Try add oracle response payload
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="response">Response</param>
        /// <returns>True if it was added</returns>
        private bool TryAddOracleResponse(StoreView snapshot, ResponseItem response)
        {
            if (!response.Msg.Verify(snapshot))
            {
                return false;
            }

            // Find the request tx

            if (_pendingOracleRequest.TryGetValue(response.Data.TransactionRequestHash, out var request))
            {
                // Append the signature if it's possible

                if (request.AddSinature(response))
                {
                    if (request.IsCompleted)
                    {
                        // Done! Send to mem pool

                        MemPool.TryAdd(request.ResponseTransaction.Hash, request.ResponseTransaction);
                        MemPool.TryAdd(request.RequestTransaction.Hash, request.RequestTransaction);

                        _pendingOracleRequest.TryRemove(response.Data.TransactionRequestHash, out _);
                    }

                    return true;
                }
            }

            // Save this payload for check it later

            if (_pendingOracleResponses.TryGetValue(response.Data.TransactionRequestHash, out var collection))
            {
                return collection.Add(response);
            }
            else
            {
                // TODO: This could not be thread-safe (lock?)

                return _pendingOracleResponses.TryAdd(response.Data.TransactionRequestHash, new ResponseCollection(response));
            }
        }

        /// <summary>
        /// Reverify pending responses
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="requestTx">Request transaction hash</param>
        private void ReverifyPendingResponses(StoreView snapshot, UInt256 requestTx)
        {
            // If the response is pending, we should process it now

            if (!_pendingOracleResponses.TryRemove(requestTx, out var collection))
            {
                return;
            }

            // Order by Transaction

            foreach (var entry in collection.OrderByDescending(a => a.ResponseTx != null ? 1 : 0))
            {
                TryAddOracleResponse(snapshot, entry);
            }
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString()); // TODO: remove this when fix UT

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
        public static bool FilterResponse(string input, OracleFilter filter, out string result, out long gasCost)
        {
            if (filter == null)
            {
                result = input;
                gasCost = 0;
                return true;
            }

            if (FilterResponse(Encoding.UTF8.GetBytes(input), filter, out var bufferResult, out gasCost))
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
        /// <param name="gasCost">Gas cost</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterResponse(byte[] input, OracleFilter filter, out byte[] result, out long gasCost)
        {
            if (filter == null)
            {
                result = input;
                gasCost = 0;
                return true;
            }

            // Prepare the execution

            using ScriptBuilder script = new ScriptBuilder();
            script.EmitSysCall(InteropService.Contract.CallEx, filter.ContractHash, filter.FilterMethod, input, (byte)CallFlags.None);

            // Execute

            using var engine = new ApplicationEngine(TriggerType.Application, null, null, MaxGasFilter, false, null);

            engine.LoadScript(script.ToArray(), CallFlags.None);

            if (engine.Execute() != VMState.HALT || engine.ResultStack.Count != 1)
            {
                result = null;
                gasCost = engine.GasConsumed;
                return false;
            }

            // Extract the filtered item

            result = engine.ResultStack.Pop().GetSpan().ToArray();
            gasCost = engine.GasConsumed;
            return true;
        }

        #endregion

        public static Props Props(IActorRef localNode, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new OracleService(localNode, wallet)).WithMailbox("oracle-service-mailbox");
        }

        internal class OracleServiceMailbox : PriorityMailbox
        {
            public OracleServiceMailbox(Settings settings, Config config) : base(settings, config) { }

            internal protected override bool IsHighPriority(object message)
            {
                switch (message)
                {
                    case Transaction _:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
