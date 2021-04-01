using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;

namespace Neo.Plugins
{
    public interface IP2PPlugin
    {
        bool OnP2PMessage(Message message);
        bool OnConsensusMessage(ConsensusPayload payload);
    }
}
