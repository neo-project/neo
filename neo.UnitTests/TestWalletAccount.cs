using Moq;
using Neo.SmartContract;
using Neo.Wallets;
using System;

namespace Neo.UnitTests
{
    class TestWalletAccount : WalletAccount
    {
        private static readonly KeyPair Key;

        public override bool HasKey => true;
        public override KeyPair GetKey() => Key;

        public TestWalletAccount(UInt160 hash)
            : base(hash)
        {
            var mock = new Mock<Contract>();
            mock.SetupGet(p => p.ScriptHash).Returns(hash);
            mock.Object.Script = Contract.CreateSignatureRedeemScript(Key.PublicKey);
            mock.Object.ParameterList = new[] { ContractParameterType.Signature };
            Contract = mock.Object;
        }

        static TestWalletAccount()
        {
            Random random = new Random();
            byte[] prikey = new byte[32];
            random.NextBytes(prikey);
            Key = new KeyPair(prikey);
        }
    }
}
