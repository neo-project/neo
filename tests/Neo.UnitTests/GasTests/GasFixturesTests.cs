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
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
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

            var persistingBlock = new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = null!,
                    Index = 1,
                    NextConsensus = null!,
                    Witness = null!
                },
                Transactions = null!
            };

            // Set state

            if (fixture.Environment != null)
            {
                // Set fee values

                using var engine = ApplicationEngine.Create(TriggerType.Application,
                  new Nep17NativeContractExtensions.ManualWitness(NativeContract.NEO.GetCommitteeAddress(snapshot)),
                  snapshot, persistingBlock, settings: TestProtocolSettings.Default);

                // Build set script

                var script = new ScriptBuilder();
                script.EmitDynamicCall(NativeContract.Policy.Hash, "setFeePerByte", fixture.Environment.Policy.FeePerByte);
                script.EmitDynamicCall(NativeContract.Policy.Hash, "setStoragePrice", fixture.Environment.Policy.StorageFee);
                script.EmitDynamicCall(NativeContract.Policy.Hash, "setExecFeeFactor", fixture.Environment.Policy.ExecutionFee);

                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());

                // Check storage

                if (fixture.Environment.Storage != null)
                {
                    foreach (var preStore in fixture.Environment.Storage)
                    {
                        var key = new StorageKey(Convert.FromBase64String(preStore.Key));
                        var value = Convert.FromBase64String(preStore.Value);

                        snapshot.Add(key, value);
                    }
                }
            }

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
