using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_HighPriorityAttribute
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Size_Get()
        {
            var test = new HighPriorityAttribute();
            test.Size.Should().Be(1);
        }

        [TestMethod]
        public void Verify()
        {
            var test = new HighPriorityAttribute();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            Assert.IsFalse(test.Verify(snapshot, new Transaction() { Signers = new Signer[] { } }));
            Assert.IsFalse(test.Verify(snapshot, new Transaction() { Signers = new Signer[] { new Signer() { Account = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01") } } }));
            Assert.IsTrue(test.Verify(snapshot, new Transaction() { Signers = new Signer[] { new Signer() { Account = NativeContract.NEO.GetCommitteeAddress(snapshot) } } }));
        }
    }
}
