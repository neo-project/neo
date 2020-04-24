using System.Numerics;
using System.Globalization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.SmartContract;
using Neo.Cryptography;
using Neo.Wallets;
using Neo.Cryptography.ECC;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void GetHashData()
        {
            TestVerifiable verifiable = new TestVerifiable();
            byte[] res = verifiable.GetHashData();
            res.Length.Should().Be(8);
            byte[] requiredData = new byte[] { 7, 116, 101, 115, 116, 83, 116, 114 };
            for (int i = 0; i < requiredData.Length; i++)
            {
                res[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void Sign()
        {
            TestVerifiable verifiable = new TestVerifiable();
            byte[] res = verifiable.Sign(new KeyPair(TestUtils.GetByteArray(32, 0x42)));
            res.Length.Should().Be(64);
        }

        [TestMethod]
        public void ToScriptHash()
        {
            byte[] testByteArray = TestUtils.GetByteArray(64, 0x42);
            UInt160 res = testByteArray.ToScriptHash();
            res.Should().Be(UInt160.Parse("2d3b96ae1bcc5a585e075e3b81920210dec16302"));
        }

        [TestMethod()]
        public void Keccak256Test()
        {
            byte[] messageA = System.Text.Encoding.Default.GetBytes("HelloWorld");
            byte[] messageB = System.Text.Encoding.Default.GetBytes("HiWorld");
            Assert.IsFalse(messageA.Keccak256().Equals(messageB.Keccak256()));
            Assert.IsTrue(messageA.Keccak256().Length == 32 && messageB.Keccak256().Length == 32);
            Assert.IsTrue(messageA.Keccak256().ToHexString() == "7c5ea36004851c764c44143b1dcb59679b11c9a68e5f41497f6cf3d480715331");
        }
    }
}
