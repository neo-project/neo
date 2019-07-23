using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.SmartContract;
using Neo.Wallets;
using System.Linq;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_DataCache
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestFind()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            var storages = snapshot.Storages;

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);

            storages.Add
                (
                new Ledger.StorageKey() { Key = new byte[] { 0x00, 0x01 }, ScriptHash = UInt160.Zero },
                new Ledger.StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new Ledger.StorageKey() { Key = new byte[] { 0x00, 0x03 }, ScriptHash = UInt160.Zero },
                new Ledger.StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new Ledger.StorageKey() { Key = new byte[] { 0x00, 0x02 }, ScriptHash = UInt160.Zero },
                new Ledger.StorageItem() { IsConstant = false, Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                storages.Find(new byte[] { 0x00 }).Select(u => u.Key.Key[1]).ToArray(),
                new byte[] { 0x01, 0x02, 0x03 }
                );

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);
        }
    }
}
