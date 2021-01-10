using Neo.Ledger;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static readonly UInt160[] DefaultExtensibleWitnessWhiteList;

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem();

            // Ensure that blockchain is loaded

            var bc = Blockchain.Singleton;

            DefaultExtensibleWitnessWhiteList = (typeof(Blockchain).GetField("extensibleWitnessWhiteList",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bc) as ImmutableHashSet<UInt160>).ToArray();
            AddWhiteList(DefaultExtensibleWitnessWhiteList); // Add other address
        }

        public static void InitializeMockNeoSystem() { }

        public static void AddWhiteList(params UInt160[] address)
        {
            var builder = ImmutableHashSet.CreateBuilder<UInt160>();
            foreach (var entry in address) builder.Add(entry);

            typeof(Blockchain).GetField("extensibleWitnessWhiteList", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Blockchain.Singleton, builder.ToImmutable());
        }
    }
}
