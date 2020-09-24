using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_LogEventArgs
    {
        [TestMethod]
        public void TestGeneratorAndGet()
        {
            IWitnessed container = new Header(ProtocolSettings.Default.Magic);
            UInt160 scripthash = UInt160.Zero;
            string message = "lalala";
            LogEventArgs logEventArgs = new LogEventArgs(container, scripthash, message);
            Assert.IsNotNull(logEventArgs);
            Assert.AreEqual(container, logEventArgs.ScriptContainer);
            Assert.AreEqual(scripthash, logEventArgs.ScriptHash);
            Assert.AreEqual(message, logEventArgs.Message);
        }
    }
}
