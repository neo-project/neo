using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractGroup
    {
        [TestMethod]
        public void TestIsValid()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            for (int i = 0; i < privateKey.Length; i++)
                privateKey[i] = (byte)random.Next(256);
            KeyPair keyPair = new KeyPair(privateKey);
            ECPoint publicKey = ECCurve.Secp256r1.G * privateKey;
            ContractGroup contractGroup = new ContractGroup
            {
                PubKey = publicKey,
                Signature = new byte[20]
            };
            Assert.AreEqual(false, contractGroup.IsValid(UInt160.Zero));
        }
    }
}
