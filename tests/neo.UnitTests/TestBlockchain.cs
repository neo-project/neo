using Akka.Actor;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Linq;
using static Neo.Ledger.Blockchain;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static readonly UInt160[] DefaultExtensibleWitnessWhiteList;

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem(ProtocolSettings.Default, null, null);
        }

        public static void InitializeMockNeoSystem()
        {
            TheNeoSystem.Blockchain.Ask<FillCompleted>(new FillMemoryPool { Transactions = Enumerable.Empty<Transaction>() }).Wait();
        }

        internal static DataCache GetTestSnapshot()
        {
            return TheNeoSystem.GetSnapshot().CreateSnapshot();
        }
    }
}
