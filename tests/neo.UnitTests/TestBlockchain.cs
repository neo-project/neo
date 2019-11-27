using Neo.Ledger;
using Neo.Persistence;
using System;
using MemoryStore = Neo.Persistence.Memory.Store;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        public static readonly IStore Store;
        public static readonly NeoSystem TheNeoSystem;

        static TestBlockchain()
        {
            Store = new MemoryStore();

            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem(Store);

            // Ensure that blockchain is loaded

            var _ = Blockchain.Singleton;
        }

        public static void InitializeMockNeoSystem()
        {
        }
    }
}
