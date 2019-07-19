using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.SQLite;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Address
    {
        [TestMethod]
        public void TestGenerator()
        {
            Address address = new Address();
            Assert.IsNotNull(address);
        }

        [TestMethod]
        public void TestGetScriptHash()
        {
            Address address = new Address();
            address.ScriptHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(address.ScriptHash));
        }

        [TestMethod]
        public void TestSetScriptHash()
        {
            Address address = new Address();
            address.ScriptHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(address.ScriptHash));
        }
    }
}
