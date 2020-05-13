using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_Nep11Token : TestKit
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        private static readonly TestNep11Token test = new TestNep11Token();
    }

    public class TestNep11Token : Nep11Token<TestNep11TokenState, Nep11AccountState>
    {
        public override int Id => 0x10000006;

        public override string Name => "testNep11Token";

        public override string Symbol => "tt";

        public override byte Decimals => 0;

        public override string ServiceName => "testNep11Token";
    }

    public class TestNep11TokenState : Nep11TokenState
    {
        public override void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Name = System.Text.Encoding.UTF8.GetString(@struct[0].GetSpan().ToArray()).ToLower();
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = new Struct(referenceCounter);
            @struct.Add(System.Text.Encoding.UTF8.GetBytes(Name));
            return @struct;
        }
    }
}
