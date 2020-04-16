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

        internal PoolItem(Transaction tx)
        {
            Tx = tx;
            Timestamp = TimeProvider.Current.UtcNow;
            LastBroadcastTimestamp = Timestamp;
        }

        internal class OracleState
        {
            public HashSet<UInt256> AllowedRequests = new HashSet<UInt256>();
        }

        public bool IsReady(StoreView snapshot, OracleState oracle)
        {
            switch (Tx.Version)
            {
                case TransactionVersion.OracleRequest:
                    {
                        if (oracle.AllowedRequests.Contains(Tx.Hash))
                        {
                            // The response was already fetched, we can put request and response in the same block

                            return true;
                        }
                        else
                        {
                            if (NativeContract.Oracle.ContainsResponse(snapshot, Tx.Hash))
                            {
                                // The response it's waiting to be consumed (block+n)

                                return true;
                            }
                            else
                            {
                                // If the response it's in the pool it's located after the request
                                // We can sort the pool by OracleResponses, but it's not needed

                                return false;
                            }
                        }
                    }
                case TransactionVersion.OracleResponse:
                    {
                        oracle.AllowedRequests.Add(Tx.OracleRequestTx);
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
