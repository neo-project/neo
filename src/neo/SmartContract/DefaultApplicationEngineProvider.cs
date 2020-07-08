using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;

namespace Neo.SmartContract
{
    class DefaultApplicationEngineProvider : IApplicationEngineProvider
    {
        static Lazy<IApplicationEngineProvider> @default = new Lazy<IApplicationEngineProvider>(() => new DefaultApplicationEngineProvider());
        static IApplicationEngineProvider Default => @default.Value;

        private DefaultApplicationEngineProvider()
        {
        }

        public ApplicationEngine Create(TriggerType trigger, IVerifiable container, StoreView snapshot, long gas, bool testMode = false)
            => new ApplicationEngine(trigger, container, snapshot, gas, testMode);
    }
}
