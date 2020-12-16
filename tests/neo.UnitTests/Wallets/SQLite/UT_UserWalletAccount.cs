using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.SQLite;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_UserWalletAccount
    {
        [TestMethod]
        public void TestGenerator()
        {
            UserWalletAccount account = new UserWalletAccount(UInt160.Zero);
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestGetHasKey()
        {
            UserWalletAccount account = new UserWalletAccount(UInt160.Zero);
            Assert.AreEqual(false, account.HasKey);
        }

        [TestMethod]
        public void TestGetKey()
        {
            UserWalletAccount account = new UserWalletAccount(UInt160.Zero);
            Assert.AreEqual(null, account.GetKey());
        }
    }
}
