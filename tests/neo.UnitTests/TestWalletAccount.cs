using Moq;
using Neo.SmartContract;
using Neo.Wallets;
using System;

namespace Neo.UnitTests
{
    class TestWalletAccount : WalletAccount
    {
        private static readonly KeyPair key;

        public override bool HasKey => true;
        public override KeyPair GetKey() => key;

        public TestWalletAccount(UInt160 hash)
            : base(hash, ProtocolSettings.Default)
        {
            var mock = new Mock<Contract>();
            mock.SetupGet(p => p.ScriptHash).Returns(hash);
            mock.Object.Script = Contract.CreateSignatureRedeemScript(key.PublicKey);
            mock.Object.ParameterList = new[] { ContractParameterType.Signature };
            Contract = mock.Object;
        }

        static TestWalletAccount()
        {
            Random random = new Random();
            byte[] prikey = new byte[32];
            random.NextBytes(prikey);
            key = new KeyPair(prikey);
        }
    }
}
