using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;

namespace Neo.Ledger
{
    /// <summary>
    /// Represents an item in the Memory Pool.
    ///
    //  Note: PoolItem objects don't consider transaction priority (low or high) in their compare CompareTo method.
    ///       This is because items of differing priority are never added to the same sorted set in MemoryPool.
    /// </summary>
    internal class PoolItem : IComparable<PoolItem>
    {
        /// <summary>
        /// Internal transaction for PoolItem
        /// </summary>
        public readonly Transaction Tx;

        /// <summary>
        /// Timestamp when transaction was stored on PoolItem
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Timestamp when this transaction was last broadcast to other nodes
        /// </summary>
        public DateTime LastBroadcastTimestamp;

        /// <summary>
        /// Check if already was found the oracle response in the storage
        /// </summary>
        private bool _responseExists;

        internal PoolItem(Transaction tx)
        {
            Tx = tx;
            Timestamp = TimeProvider.Current.UtcNow;
            LastBroadcastTimestamp = Timestamp;
            _responseExists = false;
        }

        internal class DelayState
        {
            public HashSet<UInt256> Allowed = new HashSet<UInt256>();
            public HashSet<Transaction> Delayed = new HashSet<Transaction>();
        }

        public void CheckOracleResponse(StoreView snapshot)
        {
            if (!_responseExists &&
                Tx.Version == TransactionVersion.OracleRequest &&
                NativeContract.Oracle.ContainsResponse(snapshot, Tx.Hash))
            {
                _responseExists = true;
            }
        }

        public bool IsReady(DelayState state)
        {
            switch (Tx.Version)
            {
                case TransactionVersion.OracleRequest:
                    {
                        if (state.Allowed.Remove(Tx.Hash))
                        {
                            // The response was already fetched, we can put request and response in the same block

                            return true;
                        }
                        else
                        {
                            if (_responseExists)
                            {
                                // The response it's waiting to be consumed (block+n)

                                return true;
                            }
                            else
                            {
                                // If the response it's in the pool it's located after the request
                                // We save the request in order to put after the response

                                state.Delayed.Add(Tx);
                                return false;
                            }
                        }
                    }
                case TransactionVersion.OracleResponse:
                    {
                        state.Allowed.Add(Tx.OracleRequestTx);
                        break;
                    }
            }

            return true;
        }

        public int CompareTo(Transaction otherTx)
        {
            if (otherTx == null) return 1;
            // Fees sorted ascending
            int ret = Tx.FeePerByte.CompareTo(otherTx.FeePerByte);
            if (ret != 0) return ret;
            ret = Tx.NetworkFee.CompareTo(otherTx.NetworkFee);
            if (ret != 0) return ret;
            // Transaction hash sorted descending
            return otherTx.Hash.CompareTo(Tx.Hash);
        }

        public int CompareTo(PoolItem otherItem)
        {
            if (otherItem == null) return 1;
            return CompareTo(otherItem.Tx);
        }
    }
}
