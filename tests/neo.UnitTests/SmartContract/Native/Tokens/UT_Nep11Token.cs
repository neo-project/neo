using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Native.Tokens;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;

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
        public void TestProperties()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            VM.Types.Array array = new VM.Types.Array();
            array.Add(UInt256.Zero.ToArray());
            Action action = () => test.Properties(ae, array);
            action.Should().Throw<NotImplementedException>();
        }

        [TestMethod]
        public void TestTotalSupply()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            BigInteger result = test.TotalSupply(ae, null);
            result.Should().Be(0);
        }

        [TestMethod]
        public void TestNameMethod()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            StackItem stackItem = test.NameMethod(ae, null);
            stackItem.GetString().Should().Be("testNep11Token");
        }

        [TestMethod]
        public void TestSymbolMethod()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            StackItem stackItem = test.SymbolMethod(ae, null);
            stackItem.GetString().Should().Be("tt");
        }

        [TestMethod]
        public void TestDecimalsMethod()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            StackItem stackItem = test.DecimalsMethod(ae, null);
            stackItem.GetBigInteger().Should().Be(0);
        }

        [TestMethod]
        public void TestBalanceOfMethod()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            VM.Types.Array array = new VM.Types.Array();
            array.Add(UInt160.Zero.ToArray());
            array.Add(UInt160.Zero.ToArray());
            BigInteger result = test.BalanceOf(ae, array);
            result.Should().Be(0);
        }

        [TestMethod]
        public void TestTokensOfMethod()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            //mint
            Action action = () => test.Mint(ae, UInt160.Zero, new TestNep11TokenState { Id = UInt256.Zero.ToArray() });
            action.Should().NotThrow<Exception>();
            //tokensOf
            VM.Types.Array array = new VM.Types.Array();
            array.Add(UInt160.Zero.ToArray());
            IEnumerator enumerator = test.TokensOf(ae, array);
            enumerator.Next().Should().BeTrue();
            enumerator.Value().GetSpan().ToArray().Should().BeEquivalentTo(UInt256.Zero.ToArray());
        }

        [TestMethod]
        public void TestOwnerOfMethod()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            //mint
            Action action = () => test.Mint(ae, UInt160.Zero, new TestNep11TokenState { Id = UInt256.Zero.ToArray() });
            action.Should().NotThrow<Exception>();
            //ownerOf
            VM.Types.Array array = new VM.Types.Array();
            array.Add(UInt256.Zero.ToArray());
            IEnumerator enumerator = test.OwnerOf(ae, array);
            enumerator.Next().Should().BeTrue();
            enumerator.Value().GetSpan().ToArray().Should().BeEquivalentTo(UInt160.Zero.ToArray());
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
            VM.Types.Array array = new VM.Types.Array();
            array.Add(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray());
            array.Add(UInt256.Zero.ToArray());
            action = () => test.Transfer(ae, array);
            action.Should().Equals(false);

            //transfer
            array.Add(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray());
            array.Add(UInt256.Zero.ToArray());
            action = () => test.Transfer(ae, array);
            action.Should().NotThrow<Exception>();

            //transfer no witness wrong
            array.Add(UInt160.Zero.ToArray());
            array.Add(UInt256.Zero.ToArray());
            action = () => test.Transfer(ae, array);
            action.Should().Equals(false);

            //transfer no token wrong
            array.Add(UInt160.Zero.ToArray());
            array.Add(test.GetInnerKey(UInt256.Zero.ToArray()).ToArray());
            action = () => test.Transfer(ae, array);
            action.Should().Equals(false);
        }
    }

    public class TestNep11Token : Nep11Token<TestNep11TokenState, AccountState>
    {
        public override int Id => 0x10000006;

        public override string Name => "testNep11Token";

        public override string Symbol => "tt";

        public override byte Decimals => 0;

        public override JObject Properties(StoreView snapshot, byte[] tokenid)
        {
            throw new NotImplementedException();
        }

        public StackItem NameMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return Name;
        }

        public StackItem SymbolMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return Symbol;
        }

        public StackItem DecimalsMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return new byte[] { Decimals };
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
