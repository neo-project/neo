using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.SQLite;
using System.Text;

namespace Neo.UnitTests.Wallets.SQLite
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
        public void TestSetAndGetScriptHash()
        {
            Address address = new Address
            {
                ScriptHash = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(address.ScriptHash));
        }
    }
}
