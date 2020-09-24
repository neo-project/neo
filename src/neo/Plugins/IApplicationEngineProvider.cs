using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;

namespace Neo.Plugins
{
    public interface IApplicationEngineProvider
    {
        ApplicationEngine Create(TriggerType trigger, IWitnessed container, StoreView snapshot, long gas);
    }
}
