// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ApplicationEngineProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngineProvider
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationEngine.Provider = null;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ApplicationEngine.Provider = null;
        }

        [TestMethod]
        public void TestSetAppEngineProvider()
        {
            ApplicationEngine.Provider = new TestProvider();

            using var appEngine = ApplicationEngine.Create(TriggerType.Application, null, null, gas: 0, settings: TestBlockchain.TheNeoSystem.Settings);
            (appEngine is TestEngine).Should().BeTrue();
        }

        [TestMethod]
        public void TestDefaultAppEngineProvider()
        {
            using var appEngine = ApplicationEngine.Create(TriggerType.Application, null, null, gas: 0, settings: TestBlockchain.TheNeoSystem.Settings);
            (appEngine is ApplicationEngine).Should().BeTrue();
        }

        class TestProvider : IApplicationEngineProvider
        {
            public ApplicationEngine Create(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, ProtocolSettings settings, long gas, IDiagnostic diagnostic, JumpTable jumpTable)
            {
                return new TestEngine(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic, jumpTable);
            }
        }

        class TestEngine : ApplicationEngine
        {
            public TestEngine(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, ProtocolSettings settings, long gas, IDiagnostic diagnostic, JumpTable jumpTable)
                : base(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic, jumpTable)
            {
            }
        }
    }
}
