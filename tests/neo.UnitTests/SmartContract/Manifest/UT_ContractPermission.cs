using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.SmartContract.Manifest;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractPermission
    {
        [TestMethod]
        public void TestIsAllowed()
        {
            ContractManifest contractManifest1 = TestUtils.CreateDefaultManifest();
            ContractPermission contractPermission1 = ContractPermission.DefaultPermission;
            contractPermission1.Contract = ContractPermissionDescriptor.Create(UInt160.Zero);
            Assert.AreEqual(true, contractPermission1.IsAllowed(new ContractState() { Hash = UInt160.Zero, Manifest = contractManifest1 }, "AAA"));
            contractPermission1.Contract = ContractPermissionDescriptor.CreateWildcard();

            ContractManifest contractManifest2 = TestUtils.CreateDefaultManifest();
            ContractPermission contractPermission2 = ContractPermission.DefaultPermission;
            contractPermission2.Contract = ContractPermissionDescriptor.Create(UInt160.Parse("0x0000000000000000000000000000000000000001"));
            Assert.AreEqual(false, contractPermission2.IsAllowed(new ContractState() { Hash = UInt160.Zero, Manifest = contractManifest2 }, "AAA"));
            contractPermission2.Contract = ContractPermissionDescriptor.CreateWildcard();

            Random random3 = new Random();
            byte[] privateKey3 = new byte[32];
            random3.NextBytes(privateKey3);
            ECPoint publicKey3 = ECCurve.Secp256r1.G * privateKey3;
            ContractManifest contractManifest3 = TestUtils.CreateDefaultManifest();
            contractManifest3.Groups = new ContractGroup[] { new ContractGroup() { PubKey = publicKey3 } };
            ContractPermission contractPermission3 = ContractPermission.DefaultPermission;
            contractPermission3.Contract = ContractPermissionDescriptor.Create(publicKey3);
            Assert.AreEqual(true, contractPermission3.IsAllowed(new ContractState() { Hash = UInt160.Zero, Manifest = contractManifest3 }, "AAA"));
            contractPermission3.Contract = ContractPermissionDescriptor.CreateWildcard();

            Random random4 = new Random();
            byte[] privateKey41 = new byte[32];
            random4.NextBytes(privateKey41);
            ECPoint publicKey41 = ECCurve.Secp256r1.G * privateKey41;
            byte[] privateKey42 = new byte[32];
            random4.NextBytes(privateKey42);
            ECPoint publicKey42 = ECCurve.Secp256r1.G * privateKey42;
            ContractManifest contractManifest4 = TestUtils.CreateDefaultManifest();
            contractManifest4.Groups = new ContractGroup[] { new ContractGroup() { PubKey = publicKey42 } };
            ContractPermission contractPermission4 = ContractPermission.DefaultPermission;
            contractPermission4.Contract = ContractPermissionDescriptor.Create(publicKey41);
            Assert.AreEqual(false, contractPermission4.IsAllowed(new ContractState() { Hash = UInt160.Zero, Manifest = contractManifest4 }, "AAA"));
            contractPermission4.Contract = ContractPermissionDescriptor.CreateWildcard();
        }
    }
}
