using Neo.Persistence;
using System;
using System.Collections.Immutable;
using Neo.Cryptography.ECC;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static readonly UInt160[] DefaultExtensibleWitnessWhiteList;

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem(TestProtocolSettings.Default, null, null);
        }

        internal static DataCache GetTestSnapshot()
        {
            return TheNeoSystem.GetSnapshot().CreateSnapshot();
        }
    }
}
