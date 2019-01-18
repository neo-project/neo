using Neo.Network.P2P.Payloads;

namespace Neo.Plugins
{
    public interface IConsensusMetricsPlugin
    {
        void AnalyzePrepareRequestReceived(ConsensusPayload payload);
    }
}
