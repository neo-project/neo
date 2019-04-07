using Moq;
using Neo.Cryptography.ECC;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using System;

namespace Neo.UnitTests
{
    public static class TestExecutionEngine
    {
        public static ExecutionEngine CreateTestExecutionEngine()
        {
            var mockIScriptContainer = new Mock<IScriptContainer>();
            var mockICrypto = new Mock<ICrypto>();
            ExecutionEngine engine = new ExecutionEngine(mockIScriptContainer.Object, mockICrypto.Object);
            // initialize current context
            engine.LoadScript(new byte[0]);
            return engine;
        }
    }
}
