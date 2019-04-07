using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.SmartContract;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests
{

    [TestClass]
    public class NeoServiceApplication
    {
        NeoService uut;
        Snapshot snapshot;

        [TestInitialize]
        public void TestSetup()
        {
            snapshot = TestBlockchain.CreateMockSnapshot().Object;
            uut = new NeoService(TriggerType.Application, snapshot);
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void Neo_Blockchain_GetValidators()
        {
            snapshot.GetValidators().Length.Should().Be(7);
            ExecutionEngine engine = TestExecutionEngine.CreateTestExecutionEngine();
            uut.Blockchain_GetValidators(engine).Should().Be(true);
            // TODO: cannot inspect inside engine, unless we have IExecutionEngine
        }

        [TestMethod]
        public void Neo_Blockchain_GetValidatorsScript()
        {
            snapshot.GetValidators().Length.Should().Be(7);
            ExecutionEngine engine = TestExecutionEngine.CreateTestExecutionEngine();
            uut.Blockchain_GetValidatorsScript(engine).Should().Be(true);
            // TODO: cannot inspect inside engine, unless we have IExecutionEngine
        }
    }
}
