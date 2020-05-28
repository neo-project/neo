using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native.Tokens;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_Nep11Token : TestKit
    {
        private static readonly TestNep11Token test = new TestNep11Token();

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestTotalSupply()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            StackItem stackItem = test.TotalSupply(ae, null);
            stackItem.GetBigInteger().Should().Be(0);
        }

        [TestMethod]
        public void TestMintAndBurn()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            //mint
            Action action = () => test.Mint(ae, UInt160.Zero, new TestNep11TokenState { Id = UInt256.Zero.ToArray() });
            action.Should().NotThrow<Exception>();

            //double mint wrong
            action = () => test.Mint(ae, UInt160.Zero, new TestNep11TokenState { Id = UInt256.Zero.ToArray() });
            action.Should().Throw<InvalidOperationException>();

            //burn
            action = () => test.Burn(ae, UInt256.Zero.ToArray());
            action.Should().NotThrow<Exception>();

            //burn no token wrong
            action = () => test.Burn(ae, test.GetInnerKey(UInt256.Zero.ToArray()).ToArray());
            action.Should().Throw<InvalidOperationException>();

            //burn no account wrong
            action = () => test.Burn(ae, UInt256.Zero.ToArray());
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void TestTransfer()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var ae = new ApplicationEngine(TriggerType.Application, new Nep5NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, 0, true);
            //mint
            Action action = () => test.Mint(ae, UInt160.Zero, new TestNep11TokenState { Id = UInt256.Zero.ToArray() });
            action.Should().NotThrow<Exception>();

            //transfer amount greater than balance wrong
            action = () => test.Transfer(ae, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"), UInt256.Zero.ToArray());
            action.Should().Equals(false);

            //transfer
            action = () => test.Transfer(ae, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"), UInt256.Zero.ToArray());
            action.Should().NotThrow<Exception>();

            //transfer no witness wrong
            action = () => test.Transfer(ae,  UInt160.Zero, UInt256.Zero.ToArray());
            action.Should().Equals(false);

            //transfer no token wrong
            action = () => test.Transfer(ae, UInt160.Zero, test.GetInnerKey(UInt256.Zero.ToArray()).ToArray());
            action.Should().Equals(false);

            //burn
            ae = new ApplicationEngine(TriggerType.Application, new Nep5NativeContractExtensions.ManualWitness(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01")), snapshot, 0, true);
            action = () => test.Burn(ae, UInt256.Zero.ToArray());
            action.Should().NotThrow<Exception>();
        }
    }

    public class TestNep11Token : Nep11Token<TestNep11TokenState, AccountState>
    {
        public override int Id => 0x10000006;

        public override string Name => "testNep11Token";

        public override string Symbol => "tt";

        public override JObject Properties(StoreView snapshot, byte[] tokenid)
        {
            throw new NotImplementedException();
        }

        public new StackItem TotalSupply(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.TotalSupply(engine, args);
        }
    }

    public class TestNep11TokenState : Nep11TokenState
    {
        public override void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Id = @struct[0].GetSpan().ToArray();
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = new Struct(referenceCounter);
            @struct.Add(Id);
            return @struct;
        }
    }
}
