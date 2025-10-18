// Copyright (C) 2015-2025 The Neo Project.
//
// GasFixturesTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.UnitTests.GasTests
{
    [TestClass]
    public class GasFixturesTests
    {
        [TestMethod]
        public void StdLibTest()
        {
            TestFixture("./GasTests/Fixtures/StdLib.json");
        }

        public static void TestFixture(string file)
        {
            var pathFile = Path.GetFullPath(file);
            var json = File.ReadAllText(pathFile);

            var store = TestBlockchain.GetTestSnapshotCache();
            var fixtures = JsonConvert.DeserializeObject<GasTestFixture[]>(json);

            foreach (var fixture in fixtures)
            {
                var snapshot = store.CloneCache();

                AssertFixture(fixture, snapshot);
            }
        }

        public static void AssertFixture(GasTestFixture fixture, DataCache snapshot)
        {
            // Set state

            if (fixture.PreExecution?.Storage != null)
            {
                foreach (var preStore in fixture.PreExecution.Storage)
                {
                    var key = new StorageKey(Convert.FromBase64String(preStore.Key));
                    var value = Convert.FromBase64String(preStore.Value);

                    snapshot.Add(key, value);
                }
            }

            var persistingBlock = new Block { Header = new Header() { Index = 1 } };

            // Signature

            List<UInt160> signatures = [];

            if (fixture.Signature != null)
            {
                if (fixture.Signature.SignedByCommittee)
                {
                    signatures.Add(NativeContract.NEO.GetCommitteeAddress(snapshot));
                }
            }

            foreach (var execute in fixture.Execute)
            {
                using var engine = ApplicationEngine.Create(TriggerType.Application,
                  new Nep17NativeContractExtensions.ManualWitness([.. signatures]), snapshot,
                  persistingBlock, settings: TestProtocolSettings.Default);

                engine.LoadScript(execute.Script);
                Assert.AreEqual(execute.State, engine.Execute());
                Assert.AreEqual(execute.Fee, engine.FeeConsumed);
            }
        }
    }
}
