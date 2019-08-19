using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.SQLite;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using Contract = Neo.SmartContract.Contract;

namespace Neo.UnitTests.Wallets.SQLite
{
    [TestClass]
    public class UT_UserWallet
    {
        private string path;
        private UserWallet wallet;
        public static string GetRandomPath()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            return Path.GetFullPath(string.Format("Wallet_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
        }

        [TestInitialize]
        public void Setup()
        {
            path = GetRandomPath();
            wallet = UserWallet.Create(path, "123456");
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestUtils.DeleteFile(path);
        }

        [TestMethod]
        public void TestGetName()
        {
            wallet.Name.Should().Be(Path.GetFileNameWithoutExtension(path));
        }

        [TestMethod]
        public void TestGetVersion()
        {
            Action action = () => wallet.Version.ToString();
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void TestCreateAndOpenSecureString()
        {
            string myPath = GetRandomPath();
            var ss = new SecureString();
            ss.AppendChar('a');
            ss.AppendChar('b');
            ss.AppendChar('c');

            var w1 = UserWallet.Create(myPath, ss);
            w1.Should().NotBeNull();

            var w2 = UserWallet.Open(myPath, ss);
            w2.Should().NotBeNull();

            ss.AppendChar('d');
            Action action = () => UserWallet.Open(myPath, ss);
            action.ShouldThrow<CryptographicException>();

            TestUtils.DeleteFile(myPath);
        }

        [TestMethod]
        public void TestOpen()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            var account = wallet.CreateAccount(privateKey);
            var w1 = UserWallet.Open(path, "123456");
            w1.Should().NotBeNull();

            Action action = () => UserWallet.Open(path, "123");
            action.ShouldThrow<CryptographicException>();
        }

        [TestMethod]
        public void TestCreateAccountAndGetByPrivateKey()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            var account = wallet.CreateAccount(privateKey);
            var dbAccount = wallet.GetAccount(account.ScriptHash);
            account.Should().Be(dbAccount);

            var account1 = wallet.CreateAccount(privateKey);
            var dbAccount1 = wallet.GetAccount(account1.ScriptHash);
            account1.Should().Be(dbAccount1);
        }

        [TestMethod]
        public void TestCreateAccountByScriptHash()
        {
            var account = wallet.CreateAccount(UInt160.Parse("0xa6ee944042f3c7ea900481a95d65e4a887320cf0"));
            var dbAccount = wallet.GetAccount(account.ScriptHash);
            account.Should().Be(dbAccount);
        }

        [TestMethod]
        public void TestCreateAccountBySmartContract()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract = new VerificationContract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            var account = wallet.CreateAccount(contract, key);
            var dbAccount = wallet.GetAccount(account.ScriptHash);
            account.Should().Be(dbAccount);

            byte[] privateKey2 = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey2);
            }
            KeyPair key2 = new KeyPair(privateKey2);
            Contract contract2 = new Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key2.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            var account2 = wallet.CreateAccount(contract2, key2);
            var dbAccount2 = wallet.GetAccount(account2.ScriptHash);
            account2.Should().Be(dbAccount2);
        }

        [TestMethod]
        public void TestDeleteAccount()
        {
            bool ret = wallet.DeleteAccount(UInt160.Parse("0xa6ee944042f3c7ea900481a95d65e4a887320cf0"));
            ret.Should().BeFalse();

            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            var account = wallet.CreateAccount(privateKey);
            bool ret2 = wallet.DeleteAccount(account.ScriptHash);
            ret2.Should().BeTrue();
        }

        [TestMethod]
        public void TestChangePassword()
        {
            wallet.ChangePassword("123455", "654321").Should().BeFalse();
            wallet.ChangePassword("123456", "654321").Should().BeTrue();
        }

        [TestMethod]
        public void TestContains()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            var account = wallet.CreateAccount(privateKey);
            wallet.Contains(account.ScriptHash).Should().BeTrue();
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            var ret = wallet.GetAccounts();
            ret.Should().BeNullOrEmpty();

            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            var account = wallet.CreateAccount(privateKey);
            ret = wallet.GetAccounts();
            foreach (var dbAccount in ret)
            {
                dbAccount.Should().Be(account);
            }
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            wallet.VerifyPassword("123456").Should().BeTrue();
            wallet.VerifyPassword("123").Should().BeFalse();
        }
    }
}
