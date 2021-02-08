using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;

namespace Neo.Plugins
{
    public interface IApplicationEngineProvider
    {
        ApplicationEngine Create(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, ProtocolSettings settings, long gas);
    }
}
