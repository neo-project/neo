using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.SQLite;
using System.Text;

namespace Neo.UnitTests.Wallets.SQLite
{
    [TestClass]
    public class UT_Contract
    {
        [TestMethod]
        public void TestGenerator()
        {
            Contract contract = new Contract();
            Assert.IsNotNull(contract);
        }

        [TestMethod]
        public void TestSetAndGetRawData()
        {
            Contract contract = new Contract
            {
                RawData = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.RawData));
        }

        [TestMethod]
        public void TestSetAndGetScriptHash()
        {
            Contract contract = new Contract
            {
                ScriptHash = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.ScriptHash));
        }

        [TestMethod]
        public void TestSetAndGetPublicKeyHash()
        {
            Contract contract = new Contract
            {
                PublicKeyHash = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.PublicKeyHash));
        }

        [TestMethod]
        public void TestSetAndGetAccount()
        {
            Contract contract = new Contract();
            Account account = new Account();
            contract.Account = account;
            Assert.AreEqual(account, contract.Account);
        }

        [TestMethod]
        public void TestSetAndGetAddress()
        {
            Contract contract = new Contract();
            Address address = new Address();
            contract.Address = address;
            Assert.AreEqual(address, contract.Address);
        }
    }
}
