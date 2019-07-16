using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Wrappers;
using Neo.Wallets.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Account
    {
        [TestMethod]
        public void TestGenerator()
        {
            Account account = new Account();
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestGetPrivateKeyEncrypted()
        {
            Account account = new Account();
            account.PrivateKeyEncrypted = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(account.PrivateKeyEncrypted));
        }

        [TestMethod]
        public void TestSetPrivateKeyEncrypted()
        {
            Account account = new Account();
            account.PrivateKeyEncrypted = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(account.PrivateKeyEncrypted));
        }

        [TestMethod]
        public void TestGetPublicKeyHash()
        {
            Account account = new Account();
            account.PublicKeyHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(account.PublicKeyHash));
        }

        [TestMethod]
        public void TestSetPublicKeyHash()
        {
            Account account = new Account();
            account.PublicKeyHash = new byte[] { 0x01 };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(account.PublicKeyHash));
        }
    }
}
