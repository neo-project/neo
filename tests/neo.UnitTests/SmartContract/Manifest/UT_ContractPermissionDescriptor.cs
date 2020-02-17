using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using System.Security.Cryptography;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractPermissionDescriptor
    {
        [TestMethod]
        public void TestCreateByECPointAndIsWildcard()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            ContractPermissionDescriptor contractPermissionDescriptor = ContractPermissionDescriptor.Create(key.PublicKey);
            Assert.IsNotNull(contractPermissionDescriptor);
            Assert.AreEqual(key.PublicKey, contractPermissionDescriptor.Group);
            Assert.AreEqual(false, contractPermissionDescriptor.IsWildcard);
        }

        [TestMethod]
        public void TestFromAndToJson()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            ContractPermissionDescriptor temp = ContractPermissionDescriptor.Create(key.PublicKey);
            ContractPermissionDescriptor result = ContractPermissionDescriptor.FromJson(temp.ToJson());
            Assert.AreEqual(null, result.Hash);
            Assert.AreEqual(result.Group, result.Group);
        }
    }
}
