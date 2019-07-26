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
        public void TestSetAndGetPrivateKeyEncrypted()
        {
            Account account = new Account
            {
                PrivateKeyEncrypted = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(account.PrivateKeyEncrypted));
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