using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Neo.IO;
using Neo.IO.Data.LevelDB;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.LevelDB;

namespace Neo.Consensus
{
    public static class Helper
    {
        /// <summary>
        /// Prefix for saving consensus state.
        /// </summary>
        public const byte CN_Context = 0xf4;

        private static readonly WriteOptions SynchronousWriteOptions = new WriteOptions { Sync = true };

        internal static void WriteContextToStore(this IConsensusContext context, Store store)
        {
            store.Put(CN_Context, new byte[0], context.ToArray(), SynchronousWriteOptions);
        }

        internal static bool LoadContextFromStore(this IConsensusContext context, Store store, bool shouldReset=true)
        {
            byte[] data = store.Get(CN_Context, new byte[0]);
            if (data != null)
            {
                if (shouldReset) context.Reset(0);
                using (MemoryStream ms = new MemoryStream(data, false))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    context.Deserialize(reader);
                    return true;
                }
            }
            return false;
        }

        internal static IEnumerable<Transaction> RetreiveTransactionsFromSavedConsensusContext(MemoryPool memoryPool, Store store, Store consensusStore)
        {
            IConsensusContext context = new ConsensusContext(null);
            context.LoadContextFromStore(consensusStore, false);
            return context.Transactions?.Values;
        }
    }
}
