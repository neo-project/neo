using System.IO;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.Persistence.LevelDB;

namespace Neo.Consensus
{
    public static class Helper
    {
        internal static void WriteContextToStore(this IConsensusContext context, Store store)
        {
            store.Put(Prefixes.CN_Context, new byte[0], context.ToArray());
        }

        internal static bool LoadContextFromStore(this IConsensusContext context, Store store)
        {
            byte[] data = store.Get(Prefixes.CN_Context, new byte[0]);
            if (data != null)
            {
                using (MemoryStream ms = new MemoryStream(data, false))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    context.Deserialize(reader);
                    return true;
                }
            }
            return false;
        }

        internal static void LoadTransactionsToMemoryPoolFromSavedConsensusContext(MemoryPool memoryPool, Store store, Store consensusStore)
        {
            IConsensusContext context = new ConsensusContext(null);
            context.LoadContextFromStore(consensusStore);
            if (context.Transactions == null) return;
            foreach (var tx in context.Transactions.Values)
            {
                if (store.ContainsTransaction(tx.Hash)) continue;
                memoryPool.TryAdd(tx.Hash, tx);
            }
        }
    }
}