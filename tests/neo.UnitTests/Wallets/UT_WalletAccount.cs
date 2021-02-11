using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.Wallets;

namespace Neo.UnitTests.Wallets
{
    public class MyWalletAccount : WalletAccount
    {
        private KeyPair key = null;
        public override bool HasKey => key != null;

        public MyWalletAccount(UInt160 scriptHash)
            : base(scriptHash, ProtocolSettings.Default)
        {
        }

        public override KeyPair GetKey()
        {
            return key;
        }

        public void SetKey(KeyPair inputKey)
        {
            key = inputKey;
        }
    }

    [TestClass]
    public class UT_WalletAccount
    {
        [TestMethod]
        public void TestGetAddress()
        {
            MyWalletAccount walletAccount = new MyWalletAccount(UInt160.Zero);
            walletAccount.Address.Should().Be("NKuyBkoGdZZSLyPbJEetheRhMjeznFZszf");
        }

        [TestMethod]
        public void TestGetWatchOnly()
        {
            MyWalletAccount walletAccount = new MyWalletAccount(UInt160.Zero);
            walletAccount.WatchOnly.Should().BeTrue();
            walletAccount.Contract = new Contract();
            walletAccount.WatchOnly.Should().BeFalse();
        }
    }
}
