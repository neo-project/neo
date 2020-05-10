using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_OracleResponseSignature
    {
        [TestMethod]
        public void TestSerialization()
        {
            OracleResponseSignature response = new OracleResponseSignature()
            {
                Signature = new byte[64],
                TransactionResponseHash = UInt256.Parse("0x557f5c9d0c865a211a749899681e5b4fbf745b3bcf0c395e6d6a7f1edb0d86f2"),
                TransactionRequestHash = UInt256.Parse("0x557f5c9d0c865a211a749899681e5b4fbf745b3bcf0c395e6d6a7f1edb0d86f1")
            };

            new Random().NextBytes(response.Signature);

            Assert.ThrowsException<ArgumentException>(() => response.Signature = new byte[1]);

            var data = response.ToArray();

            var copy = data.AsSerializable<OracleResponseSignature>();

            Assert.AreEqual(response.Size, data.Length);

            Assert.AreEqual(response.TransactionResponseHash, copy.TransactionResponseHash);
            Assert.AreEqual(response.TransactionRequestHash, copy.TransactionRequestHash);
            CollectionAssert.AreEqual(response.Signature, copy.Signature);
            Assert.AreEqual(response.Size, copy.Size);

            data[0]++;

            Assert.ThrowsException<FormatException>(() => data.AsSerializable<OracleResponseSignature>());
        }
    }
}
