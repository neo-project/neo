using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Util.Internal;
using Neo.Network.P2P;
using Neo.Persistence;
using Neo.Plugins;

namespace Neo.Ledger
{
    /// <summary>
    /// PoolItem are compared to each other only between a given class of transaction priority (low or high)
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
        /// Timestamp where this transaction was last broadcast to other nodes
        /// </summary>
        public DateTime LastBroadcastTimestamp;

        internal PoolItem(Transaction tx)
        {
            Tx = tx;
            Timestamp = TimeProvider.Current.UtcNow;
            LastBroadcastTimestamp = Timestamp;
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
