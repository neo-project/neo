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
    public class PoolItem : IComparable<PoolItem>
    {
        public readonly Transaction Transaction;
        public readonly DateTime Timestamp;
        public DateTime LastBroadcastTimestamp;

        public PoolItem(Transaction tx)
        {
            Transaction = tx;
            Timestamp = DateTime.UtcNow;
            LastBroadcastTimestamp = Timestamp;
        }

        public int CompareTo(Transaction tx)
        {
            if (tx == null) return 1;
            int ret = Transaction.FeePerByte.CompareTo(tx.FeePerByte);
            if (ret != 0) return ret;
            ret = Transaction.NetworkFee.CompareTo(tx.NetworkFee);
            if (ret != 0) return ret;

            return Transaction.Hash.CompareTo(tx.Hash);
        }

        public int CompareTo(PoolItem otherItem)
        {
            if (otherItem == null) return 1;
            return CompareTo(otherItem.Transaction);
        }
    }
}
