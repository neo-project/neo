using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_NotifyEventArgs
    {
        [TestMethod]
        public void TestGetScriptContainer()
        {
            IVerifiable container = new TestVerifiable();
            UInt160 script_hash = new byte[] { 0x00 }.ToScriptHash();
            StackItem state = new ContainerPlaceholder();
            NotifyEventArgs args = new NotifyEventArgs(container, script_hash, state);
            args.ScriptContainer.Should().Be(container);
        }
    }
}
