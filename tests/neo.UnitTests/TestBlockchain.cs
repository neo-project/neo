using Neo.Ledger;
using System;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem();

            // Ensure that blockchain is loaded

            var _ = Blockchain.Singleton;
        }

        public static void InitializeMockNeoSystem()
        {
        }
    }
}
