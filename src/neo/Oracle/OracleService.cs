using Akka.Actor;
using Neo.Cryptography.ECC;
using Neo.IO;
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Oracle
{
    public class OracleService : UntypedActor
    {
        #region Sub classes

        internal class StartMessage { public byte NumberOfTasks = 4; }
        internal class StopMessage { }

        private class RequestItem : PoolItem
        {
            // Request

            public readonly Transaction RequestTransaction;

            // Response

            public Contract Contract;
            public UInt160 ExpectedResultHash;
            public Transaction ResponseTransaction;
            private ContractParametersContext ResponseContext;

            public bool IsCompleted => ResponseContext?.Completed == true;

            public RequestItem(Transaction requestTx) : base(requestTx)
            {
                RequestTransaction = requestTx;
            }

            public bool AddSignature(ResponseItem response)
            {
                if (response.TransactionRequestHash != RequestTransaction.Hash)
                {
                    return false;
                }

                if (ResponseTransaction == null)
                {
                    if (response.Tx == null || response.Contract == null)
                    {
                        return false;
                    }

                    // Oracle service could attach the real TX

                    Contract = response.Contract;
                    ResponseTransaction = response.Tx;
                    ExpectedResultHash = response.ResultHash;
                    ResponseContext = new ContractParametersContext(response.Tx);
                }
                else
                {
                    if (response.ResultHash != ExpectedResultHash)
                    {
                        // Unexpected result

                        return false;
                    }
                }

                if (ResponseContext.AddSignature(Contract, response.OraclePub, response.Signature) == true)
                {
                    if (ResponseContext.Completed)
                    {
                        // Append the witness to the response TX

                        ResponseTransaction.Witnesses = ResponseContext.GetWitnesses();
                    }
                    return true;
                }

                return false;
            }
        }

        private class ResponseCollection : IEnumerable<ResponseItem>
        {
            public readonly DateTime Timestamp;

            private readonly SortedConcurrentDictionary<ECPoint, ResponseItem> _items;

            public int Count => _items.Count;

            public int MineCount { get; private set; }

            public ResponseCollection(ResponseItem item)
            {
                Timestamp = item.Timestamp;
                _items = new SortedConcurrentDictionary<ECPoint, ResponseItem>
                    (
                    Comparer<KeyValuePair<ECPoint, ResponseItem>>.Create(Sort), 1_000
                    );

                Add(item);
            }

            public bool Add(ResponseItem item)
            {
                // Prevent duplicate messages using the publicKey as key

                if (_items.TryGetValue(item.OraclePub, out var prev))
                {
                    // If it's new, replace it

                    if (prev.Timestamp > item.Timestamp) return false;

                    if (!prev.IsMine && item.IsMine) MineCount++;
                    else if (prev.IsMine && !item.IsMine) MineCount--;

                    _items.Set(item.OraclePub, item);
                    return true;
                }

                if (_items.TryAdd(item.OraclePub, item))
                {
                    if (item.IsMine) MineCount++;
                    return true;
                }

                return false;
            }

            public IEnumerator<ResponseItem> GetEnumerator()
            {
                return (IEnumerator<ResponseItem>)_items.Select(u => u.Value).ToArray().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class ResponseItem : PoolItem
        {
            private readonly OraclePayload Msg;
            private readonly OracleResponseSignature Data;

            public readonly Contract Contract;
            public ECPoint OraclePub => Msg.OraclePub;
            public UInt160 MsgHash => Msg.Hash;
            public byte[] Signature => Data.Signature;
            public UInt160 ResultHash => Data.OracleExecutionCacheHash;
            public UInt256 TransactionRequestHash => Data.TransactionRequestHash;
            public bool IsMine { get; }

            public ResponseItem(OraclePayload payload, Contract contract = null, Transaction responseTx = null) : base(responseTx)
            {
                IsMine = responseTx != null && contract != null;
                Contract = contract;
                Msg = payload;
                Data = payload.OracleSignature;
            }

            public bool Verify(StoreView snapshot)
            {
                return Msg.Verify(snapshot);
            }
        }

        private class AllowWitness : IVerifiable
        {
            public Witness[] Witnesses
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
            public int Size => throw new NotImplementedException();
            public void Deserialize(BinaryReader reader) => throw new NotImplementedException();
            public void DeserializeUnsigned(BinaryReader reader) => throw new NotImplementedException();
            public void Serialize(BinaryWriter writer) => throw new NotImplementedException();
            public void SerializeUnsigned(BinaryWriter writer) => throw new NotImplementedException();

            private readonly Contract _contract;

            public AllowWitness(Contract contract)
            {
                _contract = contract;
            }

            public UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
            {
                return new UInt160[] { _contract.ScriptHash };
            }
        }

        #endregion

        #region Protocols

        /// <summary>
        /// HTTPS protocol
        /// </summary>
        internal static OracleHttpsProtocol HTTPSProtocol { get; } = new OracleHttpsProtocol();

        #endregion

        // TODO: Fees

        private const long MaxGasFilter = 10_000_000;

        private long _isStarted = 0;
        private Contract _lastContract;
        private readonly NeoSystem _system;
        private readonly IActorRef _localNode;
        private CancellationTokenSource _cancel;
        private readonly (Contract Contract, KeyPair Key)[] _accounts;
        private readonly Func<SnapshotView> _snapshotFactory;

        /// <summary>
        /// Number of threads for processing the oracle
        /// </summary>
        private Task[] _oracleTasks;

        /// <summary>
        /// Sorted Queue for oracle tasks
        /// </summary>
        private readonly SortedBlockingCollection<UInt256, Transaction> _processingQueue;

        /// <summary>
        /// Oracle
        /// </summary>
        public Func<OracleRequest, OracleResponse> Oracle { get; }

        /// <summary>
        /// Pending user Transactions
        /// </summary>
        private readonly SortedConcurrentDictionary<UInt256, RequestItem> _pendingRequests;

        /// <summary>
        /// Pending oracle response Transactions
        /// </summary>
        private readonly SortedConcurrentDictionary<UInt256, ResponseCollection> _pendingResponses;

        /// <summary>
        /// Total maximum capacity of transactions the pool can hold.
        /// </summary>
        public int PendingCapacity => _pendingRequests.Capacity;

        /// <summary>
        /// Total requests in the pool.
        /// </summary>
        public int PendingRequestCount => _pendingRequests.Count;

        /// <summary>
        /// Total responses in the pool.
        /// </summary>
        public int PendingResponseCount => _pendingResponses.Count;

        /// <summary>
        /// Is started
        /// </summary>
        public bool IsStarted => Interlocked.Read(ref _isStarted) == 1;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="system">System</param>
        /// <param name="localNode">Local node</param>
        /// <param name="wallet">Wallet</param>
        /// <param name="snapshotFactory">Snapshot factory</param>
        /// <param name="capacity">Capacity</param>
        public OracleService(NeoSystem system, IActorRef localNode, Wallet wallet, Func<SnapshotView> snapshotFactory, int capacity)
        {
            Oracle = Process;
            _system = system;
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

            _processingQueue = new SortedBlockingCollection<UInt256, Transaction>
                (
                Comparer<KeyValuePair<UInt256, Transaction>>.Create(SortEnqueuedRequest), capacity
                );

            // Create internal collections for pending request/responses

            _pendingRequests = new SortedConcurrentDictionary<UInt256, RequestItem>
                (
                Comparer<KeyValuePair<UInt256, RequestItem>>.Create(SortRequest), capacity
                );
            _pendingResponses = new SortedConcurrentDictionary<UInt256, ResponseCollection>
                (
                Comparer<KeyValuePair<UInt256, ResponseCollection>>.Create(SortResponse), capacity
                );
        }

        /// <summary>
        /// Receive AKKA Messages
        /// </summary>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartMessage start:
                    {
                        Start(start.NumberOfTasks);
                        break;
                    }
                case StopMessage _:
                    {
                        Stop();
                        break;
                    }
                case OraclePayload msg:
                    {
                        using var snapshot = _snapshotFactory();
                        TryAddOracleResponse(snapshot, new ResponseItem(msg));
                        break;
                    }
                case Transaction tx:
                    {
                        // We only need to take care about the requests

                        if (tx.Version == TransactionVersion.OracleRequest)
                        {
                            // If it's an OracleRequest and it's new, tell it to OracleService

                            if (_pendingRequests.TryAdd(tx.Hash, new RequestItem(tx)))
                            {
                                using var snapshot = _snapshotFactory();

                                ReverifyPendingResponses(snapshot, tx.Hash);

                                // Add it to the oracle processing queue

                                _processingQueue.Add(tx.Hash, tx);
                            }
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Start oracle
        /// </summary>
        /// <param name="numberOfTasks">Number of tasks</param>
        public void Start(byte numberOfTasks = 4)
        {
            if (Interlocked.Exchange(ref _isStarted, 1) != 0) return;

            // Create tasks

            Log($"OnStart: tasks={numberOfTasks}");

            _cancel = new CancellationTokenSource();
            _oracleTasks = new Task[numberOfTasks];

            for (int x = 0; x < _oracleTasks.Length; x++)
            {
                _oracleTasks[x] = new Task(() =>
                {
                    foreach (var tx in _processingQueue.GetConsumingEnumerable(_cancel.Token))
                    {
                        ProcessRequestTransaction(tx);
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

            Log("OnStop");

            _cancel.Cancel();

            for (int x = 0; x < _oracleTasks.Length; x++)
            {
                try { _oracleTasks[x].Wait(); } catch { }
                try { _oracleTasks[x].Dispose(); } catch { }
            }

            _cancel.Dispose();
            _cancel = null;
            _oracleTasks = null;

            // Clean queue

            _processingQueue.Clear();
            _pendingRequests.Clear();
            _pendingResponses.Clear();
        }

        private static void Log(string message, LogLevel level = LogLevel.Info)
        {
            Utility.Log(nameof(OracleService), level, message);
        }

        /// <summary>
        /// Process request transaction
        /// </summary>
        /// <param name="tx">Transaction</param>
        private void ProcessRequestTransaction(Transaction tx)
        {
            Log($"Process request tx: hash={tx.Hash}");

            var oracle = new OracleExecutionCache(Process);
            using var snapshot = _snapshotFactory();
            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, false, oracle))
            {
                engine.LoadScript(tx.Script);

                if (engine.Execute() != VMState.HALT)
                {
                    // If the request TX will FAULT we can save space removing the downloaded data
                    // But user paid for it, maybe it will not fault during OnPerists

                    // oracle.Clear();
                }
            }

            // Check the oracle contract

            var contract = NativeContract.Oracle.GetOracleMultiSigContract(snapshot);

            // Check the cached contract

            if (_lastContract?.ScriptHash != contract.ScriptHash)
            {
                // Reduce the memory load using the same Contract class

                _lastContract = contract;
            }

            // Create deterministic oracle response

            var responseTx = CreateResponseTransaction(snapshot, oracle, contract, tx);

            foreach (var account in _accounts)
            {
                // Sign the transaction

                var signatureTx = responseTx.Sign(account.Key);

                // Create the payload

                var response = new OraclePayload()
                {
                    OraclePub = account.Key.PublicKey,
                    OracleSignature = new OracleResponseSignature()
                    {
                        OracleExecutionCacheHash = oracle.Hash,
                        Signature = signatureTx,
                        TransactionRequestHash = tx.Hash
                    }
                };

                var signatureMsg = response.Sign(account.Key);
                var signPayload = new ContractParametersContext(response);

                if (signPayload.AddSignature(account.Contract, response.OraclePub, signatureMsg) && signPayload.Completed)
                {
                    response.Witness = signPayload.GetWitnesses()[0];

                    if (TryAddOracleResponse(snapshot, new ResponseItem(response, contract, responseTx)))
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
        /// <param name="contract">Contract</param>
        /// <param name="requestTx">Request Hash</param>
        /// <returns>Transaction</returns>
        public static Transaction CreateResponseTransaction(SnapshotView snapshot, OracleExecutionCache oracle, Contract contract, Transaction requestTx)
        {
            using ScriptBuilder script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setOracleResponse", requestTx.Hash, IO.Helper.ToArray(oracle));

            // Calculate system fee

            long systemFee;
            using (var engine = ApplicationEngine.Run
            (
                script.ToArray(), snapshot.Clone(),
                new AllowWitness(contract), null, true, 0, null
            ))
            {
                if (engine.State != VMState.HALT)
                {
                    // This should never happend

                    throw new ApplicationException();
                }

                systemFee = engine.GasConsumed;
            }

            // Generate tx

            var tx = new Transaction()
            {
                Version = TransactionVersion.OracleResponse,
                ValidUntilBlock = requestTx.ValidUntilBlock,
                Attributes = new TransactionAttribute[0],
                OracleRequestTx = requestTx.Hash,
                Sender = contract.ScriptHash,
                Witnesses = new Witness[0],
                Script = script.ToArray(),
                NetworkFee = 0,
                Nonce = requestTx.Nonce,
                SystemFee = systemFee,
                Cosigners = new Cosigner[]
                {
                    new Cosigner()
                    {
                        Account = contract.ScriptHash,
                        AllowedContracts = new UInt160[]{ NativeContract.Oracle.Hash },
                        Scopes = WitnessScope.CustomContracts
                    }
                }
            };

            // Calculate network fee

            int size = tx.Size;

            tx.NetworkFee += Wallet.CalculateNetworkFee(contract.Script, ref size);
            tx.NetworkFee += size * NativeContract.Policy.GetFeePerByte(snapshot);

            return tx;
        }

        /// <summary>
        /// Try add oracle response payload
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="response">Response</param>
        /// <returns>True if it was added</returns>
        private bool TryAddOracleResponse(StoreView snapshot, ResponseItem response)
        {
            if (!response.Verify(snapshot))
            {
                return false;
            }

            Log($"Received oracle signature: oracle={response.OraclePub.ToString()} request={response.TransactionRequestHash} response={response.MsgHash}");

            // Find the request tx

            if (_pendingRequests.TryGetValue(response.TransactionRequestHash, out var request))
            {
                // Append the signature if it's possible

                if (request.AddSignature(response))
                {
                    if (request.IsCompleted)
                    {
                        Log($"Send response tx: hash={request.ResponseTransaction.Hash}");

                        // Done! Send to mem pool

                        _pendingRequests.TryRemove(response.TransactionRequestHash, out _);
                        _pendingResponses.TryRemove(response.TransactionRequestHash, out _);
                        _system.Blockchain.Tell(request.ResponseTransaction);

                        // Request should be already there, but it could be removed because the mempool was full during the process

                        _system.Blockchain.Tell(request.RequestTransaction);
                    }

                    return true;
                }
            }
            else
            {
                // Ask for the request tx because it's not in my pool

                _localNode.Tell(Message.Create(MessageCommand.GetData, InvPayload.Create(InventoryType.TX, response.TransactionRequestHash)));
            }

            // Save this payload for check it later

            if (_pendingResponses.TryGetValue(response.TransactionRequestHash, out var collection, new ResponseCollection(response)))
            {
                if (collection != null)
                {
                    // It was getted

                    return collection.Add(response);
                }

                // It was added

                return true;
            }

            return false;
        }

        /// <summary>
        /// Reverify pending responses
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="requestTx">Request transaction hash</param>
        private void ReverifyPendingResponses(StoreView snapshot, UInt256 requestTx)
        {
            // If the response is pending, we should process it now

            if (_pendingResponses.TryRemove(requestTx, out var collection))
            {
                // Order by Transaction

                foreach (var entry in collection)
                {
                    TryAddOracleResponse(snapshot, entry);
                }
            }
        }

        #region Sorts

        private static int Sort(KeyValuePair<ECPoint, ResponseItem> a, KeyValuePair<ECPoint, ResponseItem> b)
        {
            // Sort by if it's mine or not

            int av = a.Value.IsMine ? 1 : 0;
            int bv = b.Value.IsMine ? 1 : 0;
            int ret = av.CompareTo(bv);

            if (ret != 0) return ret;

            // Sort by time

            return a.Value.Timestamp.CompareTo(b.Value.Timestamp);
        }

        private static int SortRequest(KeyValuePair<UInt256, RequestItem> a, KeyValuePair<UInt256, RequestItem> b)
        {
            return a.Value.CompareTo(b.Value);
        }

        private static int SortEnqueuedRequest(KeyValuePair<UInt256, Transaction> a, KeyValuePair<UInt256, Transaction> b)
        {
            var otherTx = b.Value;
            if (otherTx == null) return 1;

            // Fees sorted ascending

            var tx = a.Value;
            int ret = tx.FeePerByte.CompareTo(otherTx.FeePerByte);
            if (ret != 0) return ret;
            ret = tx.NetworkFee.CompareTo(otherTx.NetworkFee);
            if (ret != 0) return ret;

            // Transaction hash sorted descending

            return otherTx.Hash.CompareTo(tx.Hash);
        }

        private static int SortResponse(KeyValuePair<UInt256, ResponseCollection> a, KeyValuePair<UInt256, ResponseCollection> b)
        {
            // Sort by number of signatures

            var comp = a.Value.Count.CompareTo(b.Value.Count);
            if (comp != 0) return comp;

            // Sort by if has my signature or not

            comp = a.Value.MineCount.CompareTo(b.Value.MineCount);
            if (comp != 0) return comp;

            // Sort by age

            return a.Value.Timestamp.CompareTo(b.Value.Timestamp);
        }

        #endregion

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

        public static Props Props(NeoSystem system, IActorRef localNode, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new OracleService(system, localNode, wallet, null, ProtocolSettings.Default.MemoryPoolMaxTransactions));
        }

        #endregion
    }
}
