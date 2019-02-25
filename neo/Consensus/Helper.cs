using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.IO;

namespace Neo.Consensus
{
    internal static class Helper
    {
        /// <summary>
        /// Prefix for saving consensus state.
        /// </summary>
        public const byte CN_Context = 0xf4;

        public static int F(this IConsensusContext context) => (context.Validators.Length - 1) / 3;
        public static int M(this IConsensusContext context) => context.Validators.Length - context.F();
        public static bool IsPrimary(this IConsensusContext context) => context.MyIndex == context.PrimaryIndex;
        public static bool IsBackup(this IConsensusContext context) => context.MyIndex >= 0 && context.MyIndex != context.PrimaryIndex;
        public static Header PrevHeader(this IConsensusContext context) => context.Snapshot.GetHeader(context.PrevHash);
        public static bool RequestSentOrReceived(this IConsensusContext context) => context.PreparationPayloads[context.PrimaryIndex] != null;
        public static bool ResponseSent(this IConsensusContext context) => context.PreparationPayloads[context.MyIndex] != null;
        public static bool CommitSent(this IConsensusContext context) => context.CommitPayloads[context.MyIndex] != null;
        public static bool BlockSent(this IConsensusContext context) => context.Block != null;
        public static bool ViewChanging(this IConsensusContext context) => context.ChangeViewPayloads[context.MyIndex]?.GetDeserializedMessage<ChangeView>().NewViewNumber > context.ViewNumber;

        public static uint GetPrimaryIndex(this IConsensusContext context, byte viewNumber)
        {
            int p = ((int)context.BlockIndex - viewNumber) % context.Validators.Length;
            return p >= 0 ? (uint)p : (uint)(p + context.Validators.Length);
        }

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
    }
}
