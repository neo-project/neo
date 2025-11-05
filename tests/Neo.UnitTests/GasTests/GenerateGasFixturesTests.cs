// Copyright (C) 2015-2025 The Neo Project.
//
// GenerateGasFixturesTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract.Native;
using Neo.VM;
using Newtonsoft.Json;

namespace Neo.UnitTests.GasTests
{
    [TestClass]
    public class GenerateGasFixturesTests
    {
        [TestMethod]
        public void StdLibTest()
        {
            var fixtureA = new GasTestFixture()
            {
                Name = "Test Stdlib with default execution fee",
                Execute =
                [
                    new ()
                    {
                        // itoa
                        Script = new ScriptBuilder().EmitDynamicCall(NativeContract.StdLib.Hash, "itoa", [1]).ToArray(),
                        Fee = 1167960
                    },
                    new ()
                    {
                        // atoi
                        Script = new ScriptBuilder().EmitDynamicCall(NativeContract.StdLib.Hash, "atoi", ["1"]).ToArray(),
                        Fee = 1047210
                    }
                ]
            };

            // With half execution fee

            var fixtureB = new GasTestFixture()
            {
                Name = "Test Stdlib with half default execution fee",
                Environment = new GasTestFixture.EnvironmentState()
                {
                    Policy = new GasTestFixture.PolicyValues()
                    {
                        ExecutionFee = 15_0000
                    }
                },
                Execute =
                [
                    new ()
                    {
                        // itoa
                        Script = new ScriptBuilder().EmitDynamicCall(NativeContract.StdLib.Hash, "itoa", [1]).ToArray(),
                        Fee = 583980
                    },
                    new ()
                    {
                        // atoi
                        Script = new ScriptBuilder().EmitDynamicCall(NativeContract.StdLib.Hash, "atoi", ["1"]).ToArray(),
                        Fee = 523605
                    }
                ]
            };

            var json = JsonConvert.SerializeObject(new GasTestFixture[] { fixtureA, fixtureB });
            Assert.IsNotNull(json);
        }
    }
}
