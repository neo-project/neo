using Akka.Actor;
using Akka.Event;
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
using System.Linq;
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

            // Proposal

            private ResponseItem Proposal;
            private ContractParametersContext ResponseContext;

            public Transaction ResponseTransaction => Proposal?.Tx;
            public bool IsErrorResponse => Proposal?.IsError == true;
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

                if (Proposal == null)
                {
                    if (!response.IsMine)
                    {
                        return false;
                    }

                    // Oracle service could attach the real TX

                    Proposal = response;
                    ResponseContext = new ContractParametersContext(response.Tx);
                }
                else
                {
                    if (response.TransactionResponseHash != Proposal.TransactionResponseHash)
                    {
                        // Unexpected result

                        return false;
                    }
                }

                if (ResponseContext.AddSignature(Proposal.Contract, response.OraclePub, response.Signature) == true)
                {
                    if (ResponseContext.Completed)
                    {
                        // Append the witness to the response TX

                        Proposal.Tx.Witnesses = ResponseContext.GetWitnesses();
                    }
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Clear responses
            /// </summary>
            public void CleanResponses()
            {
                Proposal = null;
                ResponseContext = null;
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
            public byte[] Signature => Data.Signature;
            public UInt256 TransactionResponseHash => Data.TransactionResponseHash;
            public UInt256 TransactionRequestHash => Data.TransactionRequestHash;
            public bool IsMine { get; }
            public bool IsError { get; }

            public ResponseItem(OraclePayload payload, Contract contract = null, Transaction responseTx = null, bool isError = false) : base(responseTx)
            {
                IsError = isError;
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

        private enum OracleResponseResult : byte
        {
            Invalid,
            Merged,
            Duplicated,
            RelayedTx
        }

        #endregion

        #region Protocols

        /// <summary>
        /// HTTPS protocol
        /// </summary>
        internal static OracleHttpsProtocol HTTPSProtocol { get; } = new OracleHttpsProtocol();

        #endregion

        private long _isStarted = 0;
        private Contract _lastContract;
        private readonly IActorRef _localNode;
        private readonly IActorRef _taskManager;
        private CancellationTokenSource _cancel;
        private readonly (Contract Contract, KeyPair Key)[] _accounts;
        private readonly Func<SnapshotView> _snapshotFactory;
        private static readonly TimeSpan TimeoutInterval = TimeSpan.FromMinutes(5);

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
        /// <param name="localNode">Local node</param>
        /// <param name="taskManager">Task manager</param>
        /// <param name="wallet">Wallet</param>
        /// <param name="snapshotFactory">Snapshot factory</param>
        /// <param name="capacity">Capacity</param>
        public OracleService(IActorRef localNode, IActorRef taskManager, Wallet wallet, Func<SnapshotView> snapshotFactory, int capacity)
        {
            Oracle = Process;
            _localNode = localNode;
            _taskManager = taskManager;
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
                throw new ArgumentException("The wallet doesn't have any oracle accounts");
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

            Context.System.EventStream.Subscribe<Blockchain.RelayResult>(Self);
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
                case TaskManager.Timer _:
                    {
                        // Create error response because there are no agreement

                        foreach (var request in _pendingRequests
                            .Where(u => !u.Value.IsErrorResponse && DateTime.UtcNow - u.Value.Timestamp > TimeoutInterval)
                            .ToArray()
                            )
                        {
                            request.Value.CleanResponses();
                            ProcessRequestTransaction(request.Value.RequestTransaction, true);
                        }
                        break;
                    }
                case Blockchain.RelayResult rr when rr.Result == VerifyResult.Succeed:
                    switch (rr.Inventory)
                    {
                        case OraclePayload msg:
                            using (var snapshot = _snapshotFactory())
                                TryAddOracleResponse(snapshot, new ResponseItem(msg));
                            break;
                        case Transaction tx when tx.IsOracleRequest():
                            // We only need to take care about the requests

                            if (_pendingRequests.TryAdd(tx.Hash, new RequestItem(tx)))
                            {
                                using (var snapshot = _snapshotFactory())
                                    ReverifyPendingResponses(snapshot, tx.Hash);

                                // Add it to the oracle processing queue

                                _processingQueue.Add(tx.Hash, tx);
                            }

                            break;
                    }
                    break;
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
                        ProcessRequestTransaction(tx, false);
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

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="level">Log level</param>
        private static void Log(string message, LogLevel level = LogLevel.Info)
        {
            Utility.Log(nameof(OracleService), level, message);
        }

        /// <summary>
        /// Process request transaction
        /// </summary>
        /// <param name="tx">Transaction</param>
        /// <param name="forceError">Force error</param>
        private void ProcessRequestTransaction(Transaction tx, bool forceError)
        {
            Log($"Process oracle request: requestTx={tx.Hash} forceError={forceError}");

            using var snapshot = _snapshotFactory();
            var oracle = new OracleExecutionCache(Process);

            if (!forceError)
            {
                // If we want to force the error we don't need to process the transaction

                using var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, false, oracle);
                engine.LoadScript(tx.Script);

                if (engine.Execute() != VMState.HALT)
                {
                    // If the TX request FAULT, we can save space by deleting the downloaded data
                    // but the user paid it, maybe it won't fail during OnPerist

                    // oracle.Clear();
                }
            }

            // Check the oracle contract and update the cached one

            var contract = NativeContract.Oracle.GetOracleMultiSigContract(snapshot);

            if (_lastContract?.ScriptHash != contract.ScriptHash)
            {
                // Reduce the memory load using the same Contract class

                _lastContract = contract;
            }
            else
            {
                // Use the same cached object in order to save memory in the pools

                contract = _lastContract;
            }

            // Create deterministic oracle response

            var responseTx = CreateResponseTransaction(snapshot, oracle, contract, tx);

            Log($"Generated response tx: requestTx={tx.Hash} responseTx={responseTx.Hash}");

            foreach (var account in _accounts)
            {
                // Create the payload with the signed transction

                var response = new OraclePayload()
                {
                    OraclePub = account.Key.PublicKey,
                    OracleSignature = new OracleResponseSignature()
                    {
                        TransactionResponseHash = responseTx.Hash,
                        Signature = responseTx.Sign(account.Key),
                        TransactionRequestHash = tx.Hash
                    }
                };

                var signatureMsg = response.Sign(account.Key);
                var signPayload = new ContractParametersContext(response);

                if (signPayload.AddSignature(account.Contract, response.OraclePub, signatureMsg) && signPayload.Completed)
                {
                    response.Witness = signPayload.GetWitnesses()[0];

                    switch (TryAddOracleResponse(snapshot, new ResponseItem(response, contract, responseTx, forceError)))
                    {
                        case OracleResponseResult.Merged:
                            {
                                // Send my signature by P2P

                                Log($"Send oracle signature: oracle={response.OraclePub} requestTx={tx.Hash} responseTx={response.Hash}");

                                _localNode.Tell(new LocalNode.SendDirectly { Inventory = response });
                                break;
                            }
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
        private static Transaction CreateResponseTransaction(SnapshotView snapshot, OracleExecutionCache oracle, Contract contract, Transaction requestTx)
        {
            using ScriptBuilder script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setOracleResponse", requestTx.Hash, IO.Helper.ToArray(oracle));

            // Calculate system fee

            long systemFee;
            using (var engine = ApplicationEngine.Run
            (
                script.ToArray(), snapshot.Clone(),
                new ManualWitness(contract.ScriptHash), testMode: true
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
                Version = 0,
                ValidUntilBlock = requestTx.ValidUntilBlock,
                Attributes = new TransactionAttribute[]
                {
                    new OracleResponseAttribute()
                    {
                         RequestTx = requestTx.Hash,
                    }
                },
                Signers = new Signer[]
                {
                    new Signer()
                    {
                        Account = contract.ScriptHash,
                        AllowedContracts = new UInt160[]{ NativeContract.Oracle.Hash },
                        Scopes = WitnessScope.CustomContracts
                    }
                },
                Witnesses = new Witness[0],
                Script = script.ToArray(),
                NetworkFee = 0,
                Nonce = requestTx.Nonce,
                SystemFee = systemFee
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
        private OracleResponseResult TryAddOracleResponse(StoreView snapshot, ResponseItem response)
        {
            if (!response.Verify(snapshot))
            {
                Log($"Received wrong signed payload: oracle={response.OraclePub} requestTx={response.TransactionRequestHash} responseTx={response.TransactionRequestHash}", LogLevel.Error);

                return OracleResponseResult.Invalid;
            }

            if (!response.IsMine)
            {
                Log($"Received oracle signature: oracle={response.OraclePub} requestTx={response.TransactionRequestHash} responseTx={response.TransactionRequestHash}");
            }

            // Find the request tx

            if (_pendingRequests.TryGetValue(response.TransactionRequestHash, out var request))
            {
                // Append the signature if it's possible

                if (request.AddSignature(response))
                {
                    if (request.IsCompleted)
                    {
                        Log($"Send response tx: oracle={response.OraclePub} responseTx={request.ResponseTransaction.Hash}");

                        // Done! Send to mem pool

                        _pendingRequests.TryRemove(response.TransactionRequestHash, out _);
                        _pendingResponses.TryRemove(response.TransactionRequestHash, out _);
                        _localNode.Tell(new LocalNode.SendDirectly { Inventory = request.ResponseTransaction });

                        // Request should be already there, but it could be removed because the mempool was full during the process

                        _localNode.Tell(new LocalNode.SendDirectly { Inventory = request.RequestTransaction });

                        return OracleResponseResult.RelayedTx;
                    }

                    return OracleResponseResult.Merged;
                }
            }
            else
            {
                // Ask for the request tx because it's not in my pool

                _taskManager.Tell(new TaskManager.RestartTasks
                {
                    Payload = InvPayload.Create(InventoryType.TX, response.TransactionRequestHash)
                });
            }

            // Save this payload for check it later

            if (_pendingResponses.TryGetValue(response.TransactionRequestHash, out var collection, new ResponseCollection(response)))
            {
                if (collection != null)
                {
                    // It was getted

                    return collection.Add(response) ? OracleResponseResult.Merged : OracleResponseResult.Duplicated;
                }

                // It was added

                return OracleResponseResult.Merged;
            }

            return OracleResponseResult.Duplicated;
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
            // Fees sorted ascending

            var tx = a.Value;
            int ret = tx.FeePerByte.CompareTo(b.Value.FeePerByte);
            if (ret != 0) return ret;
            ret = tx.NetworkFee.CompareTo(b.Value.NetworkFee);
            if (ret != 0) return ret;

            // Transaction hash sorted descending

            return b.Value.Hash.CompareTo(tx.Hash);
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
                    _ => OracleResponse.CreateError(request.Hash),
                };
            }
            catch
            {
                return OracleResponse.CreateError(request.Hash);
            }
        }

        #endregion

        #region Akka

        public static Props Props(IActorRef localNode, IActorRef taskManager, Wallet wallet)
        {
            return Akka.Actor.Props.Create(() => new OracleService(localNode, taskManager, wallet, null, ProtocolSettings.Default.MemoryPoolMaxTransactions));
        }

        #endregion
    }
}
