using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.SQLite;
using System.Text;

namespace Neo.UnitTests.Wallets.SQLite
{
    [TestClass]
    public class UT_Account
    {
        [TestMethod]
        public void TestGenerator()
        {
            Account account = new Account();
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestSetAndGetNep2key()
        {
            Account account = new Account
            {
                Nep2key = "123"
            };
            Assert.AreEqual("123", account.Nep2key);
        }

        [TestMethod]
        public void TestSetAndGetPublicKeyHash()
        {
            Account account = new Account
            {
                PublicKeyHash = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(account.PublicKeyHash));
        }
    }
}
