using Akka.Actor;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Ledger
{
    public class OraclePool
    {
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

            internal ResponseItem(OraclePayload payload, Transaction responseTx) : base(null)
            {
                Data = payload.OracleSignature;
                ResponseTx = responseTx;
            }
        }

        private Contract _lastContract;

        /// <summary>
        /// Actors
        /// </summary>
        private readonly IActorRef _oracleService, _localNode;

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
        public int Capacity { get; }

        /// <summary>
        /// MemoryPool
        /// </summary>
        public MemoryPool MemPool { get; }

        /// <summary>
        /// Total requests in the pool.
        /// </summary>
        public int RequestCount => _pendingOracleRequest.Count;

        /// <summary>
        /// Total responses in the pool.
        /// </summary>
        public int ResponseCount => _pendingOracleResponses.Count;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="memPool">Memory pool</param>
        /// <param name="oracleService">Oracle service</param>
        /// <param name="localNode">Local node</param>
        /// <param name="capacity">Capacity</param>
        public OraclePool(MemoryPool memPool, IActorRef oracleService, IActorRef localNode, int capacity)
        {
            Capacity = capacity;
            MemPool = memPool;
            _localNode = localNode;
            _oracleService = oracleService;

            // Create internal collections

            _pendingOracleRequest = new SortedConcurrentDictionary<UInt256, RequestItem>
                (
                Comparer<KeyValuePair<UInt256, RequestItem>>.Create(SortRequest), capacity
                );
            _pendingOracleResponses = new SortedConcurrentDictionary<UInt256, ResponseCollection>
                (
                Comparer<KeyValuePair<UInt256, ResponseCollection>>.Create(SortResponse), capacity
                );
        }

        #region Sort

        private int SortRequest(KeyValuePair<UInt256, RequestItem> a, KeyValuePair<UInt256, RequestItem> b)
        {
            return a.Value.CompareTo(b.Value);
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

        /// <summary>
        /// Try add a transaction from P2P layer
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="tx">Transaction</param>
        /// <returns>True if it was added</returns>
        internal bool TryAdd(StoreView snapshot, Transaction tx)
        {
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
                            _oracleService.Tell(tx);
                            return true;
                        }

                        break;
                    }
                case TransactionVersion.OracleResponse:
                    {
                        var hashes = tx.GetScriptHashesForVerifying(snapshot);

                        if (hashes.Length != 1 || hashes[0] != contract.ScriptHash)
                        {
                            return false;
                        }

                        // We should receive only this transactions P2P, never from OracleService

                        if (!tx.VerifyWitnesses(snapshot, 200_000_000))
                        {
                            return false;
                        }

                        ReverifyPendingResponses(snapshot, tx.OracleRequestTx);

                        // TODO: Send it to mempool?

                        return true;
                    }
            }

            return false;
        }
    }
}
