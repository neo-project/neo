using System.Linq;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.Runtime.CompilerServices;

namespace Neo.Consensus
{
    internal static class Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int F(this IConsensusContext context) => (context.Validators.Length - 1) / 3;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int M(this IConsensusContext context) => context.Validators.Length - context.F();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrimary(this IConsensusContext context) => context.MyIndex == context.PrimaryIndex;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBackup(this IConsensusContext context) => context.MyIndex >= 0 && context.MyIndex != context.PrimaryIndex;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WatchOnly(this IConsensusContext context) => context.MyIndex < 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Header PrevHeader(this IConsensusContext context) => context.Snapshot.GetHeader(context.PrevHash);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountCommitted(this IConsensusContext context) => context.CommitPayloads.Count(p => p != null);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountFailed(this IConsensusContext context) => context.LastSeenMessage.Count(p => p.Value < (((int)context.BlockIndex) - 1));

        // Consensus States
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RequestSentOrReceived(this IConsensusContext context) => context.PreparationPayloads[context.PrimaryIndex] != null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ResponseSent(this IConsensusContext context) => !context.WatchOnly() && context.PreparationPayloads[context.MyIndex] != null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CommitSent(this IConsensusContext context) => !context.WatchOnly() && context.CommitPayloads[context.MyIndex] != null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BlockSent(this IConsensusContext context) => context.Block != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ViewChanging(this IConsensusContext context) => !context.WatchOnly() && context.ChangeViewPayloads[context.MyIndex]?.GetDeserializedMessage<ChangeView>().NewViewNumber > context.ViewNumber;

        public static bool NotAcceptingPayloadsDueToViewChanging(this IConsensusContext context) => context.ViewChanging() && !context.MoreThanFNodesCommittedOrLost();

        // A possible attack can happen if the last node to commit is malicious and either sends change view after his
        // commit to stall nodes in a higher view, or if he refuses to send recovery messages. In addition, if a node
        // asking change views loses network or crashes and comes back when nodes are committed in more than one higher
        // numbered view, it is possible for the node accepting recovery to commit in any of the higher views, thus
        // potentially splitting nodes among views and stalling the network.
        public static bool MoreThanFNodesCommittedOrLost(this IConsensusContext context) => (context.CountCommitted() + context.CountFailed()) > context.F();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetPrimaryIndex(this IConsensusContext context, byte viewNumber)
        {
            int p = ((int)context.BlockIndex - viewNumber) % context.Validators.Length;
            return p >= 0 ? (uint)p : (uint)(p + context.Validators.Length);
        }
    }
}
