using Neo.Network.P2P.Payloads;
using Neo.Persistence;

namespace Neo.SmartContract
{
    public interface IApplicationEngineProvider
    {
        ApplicationEngine Create(TriggerType trigger, IVerifiable container, StoreView snapshot, long gas, bool testMode = false);
    }
}
