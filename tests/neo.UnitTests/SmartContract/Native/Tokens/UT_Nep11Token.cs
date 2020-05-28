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
            StackItem stackItem = test.TotalSupply(ae, null);
            stackItem.GetBigInteger().Should().Be(0);
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
            StackItem stackItem = test.BalanceOfMethod(ae, array);
            stackItem.GetBigInteger().Should().Be(0);
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
            StackItem stackItem = test.TokensOfMethod(ae, array);
            IEnumerator enumerator = ((InteropInterface)stackItem).GetInterface<IEnumerator>();
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
            StackItem stackItem = test.OwnerOfMethod(ae, array);
            IEnumerator enumerator = ((InteropInterface)stackItem).GetInterface<IEnumerator>();
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
            action = () => test.TransferMethod(ae, array);
            action.Should().Equals(false);

            //transfer
            array.Add(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray());
            array.Add(UInt256.Zero.ToArray());
            action = () => test.TransferMethod(ae, array);
            action.Should().NotThrow<Exception>();

            //transfer no witness wrong
            array.Add(UInt160.Zero.ToArray());
            array.Add(UInt256.Zero.ToArray());
            action = () => test.TransferMethod(ae, array);
            action.Should().Equals(false);

            //transfer no token wrong
            array.Add(UInt160.Zero.ToArray());
            array.Add(test.GetInnerKey(UInt256.Zero.ToArray()).ToArray());
            action = () => test.TransferMethod(ae, array);
            action.Should().Equals(false);
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

        public new StackItem Properties(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.Properties(engine, args);
        }

        public new StackItem TotalSupply(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.TotalSupply(engine, args);
        }

        public new StackItem NameMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.NameMethod(engine, args);
        }

        public new StackItem SymbolMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.SymbolMethod(engine, args);
        }

        public new StackItem DecimalsMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.DecimalsMethod(engine, args);
        }

        public StackItem BalanceOfMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.BalanceOf(engine, args);
        }

        public StackItem TokensOfMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.TokensOf(engine, args);
        }

        public StackItem OwnerOfMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.OwnerOf(engine, args);
        }

        public StackItem TransferMethod(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.Transfer(engine, args);
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
