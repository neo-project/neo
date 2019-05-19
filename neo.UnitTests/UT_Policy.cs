using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using System.Linq;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Policy
    {
        Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void Initialize()
        {
            var snapshot = Store.GetSnapshot().Clone();
            var keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.Policy.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0)).Should().BeTrue();

            (keyCount + 5).Should().Be(snapshot.Storages.GetChangeSet().Count());

            var ret = NativeContract.Policy.Call(snapshot, "getMaxTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(512);

            ret = NativeContract.Policy.Call(snapshot, "getMaxLowPriorityTransactionsPerBlock");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(20);

            ret = NativeContract.Policy.Call(snapshot, "getMaxLowPriorityTransactionSize");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(256);

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            ret.Should().BeOfType<VM.Types.Integer>();
            ret.GetBigInteger().Should().Be(1000);

            ret = NativeContract.Policy.Call(snapshot, "getBlockedAccounts");
            ret.Should().BeOfType<VM.Types.Array>();
            ((VM.Types.Array)ret).Count.Should().Be(0);
        }
    }
}