using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.Collections.Generic;
using System.IO;

namespace Neo.Consensus
{
    internal static class Helper
    {
        /// <summary>
        /// Prefix for saving consensus state.
        /// </summary>
        public const byte CN_Context = 0xf4;

        public static void Save(this IConsensusContext context, Store store)
        {
            store.PutSync(CN_Context, new byte[0], context.ToArray());
        }

        public static bool Load(this IConsensusContext context, Store store)
        {
            byte[] data = store.Get(CN_Context, new byte[0]);
            if (data is null) return false;
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                try
                {
                    context.Deserialize(reader);
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        public static IEnumerable<Transaction> RetreiveTransactionsFromSavedConsensusContext(Store consensusStore)
        {
            IConsensusContext context = new ConsensusContext(null);
            context.Load(consensusStore);
            return context.Transactions?.Values ?? (IEnumerable<Transaction>)new Transaction[0];
        }
    }
}
