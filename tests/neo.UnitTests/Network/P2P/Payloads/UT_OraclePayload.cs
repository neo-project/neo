using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_OraclePayload
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestSerialization()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var response = new OracleResponseSignature()
            {
                Signature = new byte[64],
                TransactionResponseHash = UInt256.Parse("0x557f5c9d0c865a211a749899681e5b4fbf745b3bcf0c395e6d6a7f1edb0d86f2"),
                TransactionRequestHash = UInt256.Parse("0x557f5c9d0c865a211a749899681e5b4fbf745b3bcf0c395e6d6a7f1edb0d86f1")
            };

            new Random().NextBytes(response.Signature);

            var payload = new OraclePayload()
            {
                Witness = new Witness()
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                },
                OracleSignature = response,
                OraclePub = NativeContract.Oracle.GetOracleValidators(snapshot)[0]
            };

            var data = payload.ToArray();
            var copy = data.AsSerializable<OraclePayload>();

            Assert.AreEqual(payload.Size, data.Length);

            CollectionAssert.AreEqual(payload.Witness.InvocationScript, copy.Witness.InvocationScript);
            CollectionAssert.AreEqual(payload.Witness.VerificationScript, copy.Witness.VerificationScript);
            CollectionAssert.AreEqual(payload.Data, copy.Data);
            Assert.AreEqual(payload.OraclePub, copy.OraclePub);
            Assert.AreEqual(payload.Size, copy.Size);
        }
    }
}
