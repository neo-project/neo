using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P;
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
            public UInt160 ExpectedResultHash;
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
                    ExpectedResultHash = response.ResultHash;
                    ResponseContext = new ContractParametersContext(response.ResponseTx);
                }
                else
                {
                    if (response.ResultHash != ExpectedResultHash)
                    {
                        // Unexpected result

                        return false;
                    }
                }

                // TODO: Check duplicate call

                return ResponseContext.AddSignature(Contract, response.OraclePub, response.Data.Signature) == true;
            }
        }

        private class ResponseCollection : IEnumerable<ResponseItem>
        {
            public readonly DateTime Timestamp;
            public readonly SortedConcurrentDictionary<UInt160, ResponseItem> Items;

            public ResponseCollection(ResponseItem item)
            {
                Timestamp = item.Timestamp;
                Items = new SortedConcurrentDictionary<UInt160, ResponseItem>
                    (
                    Comparer<KeyValuePair<UInt160, ResponseItem>>.Create(Sort), 1_000
                    );

                Add(item);
            }

            private int Sort(KeyValuePair<UInt160, ResponseItem> a, KeyValuePair<UInt160, ResponseItem> b)
            {
                // Sort by if it's mine or not

                int av = a.Value.ResponseTx != null ? 1 : 0;
                int bv = b.Value.ResponseTx != null ? 1 : 0;

                int ret = av.CompareTo(bv);

                if (ret == 0)
                {
                    // Sort by time

                    return a.Value.Timestamp.CompareTo(b.Value.Timestamp);
                }

                return ret;
            }

            public bool Add(ResponseItem item)
            {
                // Prevent duplicate messages

                return Items.TryAdd(item.MsgHash, item);
            }

            public IEnumerator<ResponseItem> GetEnumerator()
            {
                return (IEnumerator<ResponseItem>)Items.Select(u => u.Value).ToArray().GetEnumerator();
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
            public UInt160 MsgHash => Msg.Hash;
            public UInt160 ResultHash => Data.OracleExecutionCacheHash;

            internal ResponseItem(OraclePayload payload, Transaction responseTx) : base(responseTx)
            {
                Msg = payload;
                Data = payload.OracleSignature;
                ResponseTx = responseTx;
            }
        }

        #endregion

        // TODO: Fees

        private const long MaxGasFilter = 1_000_000;
        private const long TxNetworkFee = 1_000_000;
        private const long TxSystemFee = 1_000_000;

        private long _isStarted = 0;
        private Contract _lastContract;
        private readonly MemoryPool _memPool;
        private readonly IActorRef _localNode;
        private CancellationTokenSource _cancel;
        private readonly (Contract Contract, KeyPair Key)[] _accounts;
        private readonly Func<SnapshotView> _snapshotFactory;

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
        /// <param name="snapshotFactory">Snapshot factory</param>
        public OracleService(IActorRef localNode, Wallet wallet, Func<SnapshotView> snapshotFactory)
        {
            Oracle = Process;
            _memPool = Blockchain.Singleton.MemPool;
            PendingCapacity = _memPool.Capacity;
            _localNode = localNode;
            _snapshotFactory = snapshotFactory ?? new Func<SnapshotView>(() => Blockchain.Singleton.GetSnapshot());

            // Find oracle account

            using var snapshot = _snapshotFactory();
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

            // Create queue for pending request that should be processed

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
                        using var snapshot = _snapshotFactory();
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

                                    _memPool.TryAdd(tx.Hash, tx);
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Move one transaction from the sorted queue to _asyncPool, this will ensure that the threads process the
        /// transactions according to the priority
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
            using var snapshot = _snapshotFactory();
            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, false, oracle))
            {
                engine.LoadScript(tx.Script);

                if (engine.Execute() != VMState.HALT)
                {
                    // TODO: If the request TX will FAULT?

                    oracle.Clear();
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

                        _localNode.Tell(Message.Create(MessageCommand.Oracle, response));
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
                Witnesses = new Witness[0],
                Attributes = new TransactionAttribute[0],
                Version = TransactionVersion.OracleResponse,
                ValidUntilBlock = requestTx.ValidUntilBlock,
                OracleRequestTx = requestTx.Hash,
                Script = script.ToArray(),
                NetworkFee = TxNetworkFee,
                SystemFee = TxSystemFee,
                Nonce = requestTx.Nonce,
                Sender = sender,
                Cosigners = new Cosigner[]
                {
                    new Cosigner()
                    {
                        Account = sender,
                        AllowedContracts = new UInt160[]{ NativeContract.Oracle.Hash },
                        Scopes = WitnessScope.CustomContracts
                    }
                }
            };
        }

        #region Sorts

        private static int SortRequest(KeyValuePair<UInt256, RequestItem> a, KeyValuePair<UInt256, RequestItem> b)
        {
            return a.Value.CompareTo(b.Value);
        }

        private static int SortEnqueuedRequest(KeyValuePair<UInt256, Transaction> a, KeyValuePair<UInt256, Transaction> b)
        {
            return a.Value.FeePerByte.CompareTo(b.Value.FeePerByte);
        }

        private static int SortResponse(KeyValuePair<UInt256, ResponseCollection> a, KeyValuePair<UInt256, ResponseCollection> b)
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

                        _pendingOracleRequest.TryRemove(response.Data.TransactionRequestHash, out _);

                        _memPool.TryAdd(request.ResponseTransaction.Hash, request.ResponseTransaction);
                        _memPool.TryAdd(request.RequestTransaction.Hash, request.RequestTransaction);
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

            foreach (var entry in collection)
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

        #region Akka

        public static Props Props(IActorRef localNode, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new OracleService(localNode, wallet, null)).WithMailbox("oracle-service-mailbox");
        }

        internal class OracleServiceMailbox : PriorityMailbox
        {
            public OracleServiceMailbox(Settings settings, Config config) : base(settings, config) { }

            internal protected override bool IsHighPriority(object message)
            {
                return message switch
                {
                    Transaction _ => true,
                    _ => false,
                };
            }
        }

        #endregion
    }
}
