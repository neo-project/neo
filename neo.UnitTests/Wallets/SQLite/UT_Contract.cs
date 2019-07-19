using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.SQLite;
using System.Text;

namespace Neo.UnitTests
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
        public void TestGetRawData()
        {
            Contract contract = new Contract();
            contract.RawData = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.RawData));
        }

        [TestMethod]
        public void TestSetRawData()
        {
            Contract contract = new Contract();
            contract.RawData = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.RawData));
        }

        [TestMethod]
        public void TestGetScriptHash()
        {
            Contract contract = new Contract();
            contract.ScriptHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.ScriptHash));
        }

        [TestMethod]
        public void TestSetScriptHash()
        {
            Contract contract = new Contract();
            contract.ScriptHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.ScriptHash));
        }

        [TestMethod]
        public void TestGetPublicKeyHash()
        {
            Contract contract = new Contract();
            contract.PublicKeyHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.PublicKeyHash));
        }

        [TestMethod]
        public void TestSetPublicKeyHash()
        {
            Contract contract = new Contract();
            contract.PublicKeyHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(contract.PublicKeyHash));
        }

        [TestMethod]
        public void TestGetAccount()
        {
            Contract contract = new Contract();
            Account account = new Account();
            contract.Account = account;
            Assert.AreEqual(account, contract.Account);
        }

        [TestMethod]
        public void TestSetAccount()
        {
            Contract contract = new Contract();
            Account account = new Account();
            contract.Account = account;
            Assert.AreEqual(account, contract.Account);
        }

        [TestMethod]
        public void TestGetAddress()
        {
            Contract contract = new Contract();
            Address address = new Address();
            contract.Address = address;
            Assert.AreEqual(address, contract.Address);
        }

        [TestMethod]
        public void TestSetAddress()
        {
            Contract contract = new Contract();
            Address address = new Address();
            contract.Address = address;
            Assert.AreEqual(address, contract.Address);
        }
    }
}
