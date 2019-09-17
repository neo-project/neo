using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
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
            for (int i = 0; i < 4; i++)
            {
                if (i == 0)
                {
                    ContractManifest contractManifest = ContractManifest.CreateDefault(UInt160.Zero);
                    ContractPermission contractPermission = ContractPermission.DefaultPermission;
                    contractPermission.Contract = ContractPermissionDescriptor.Create(UInt160.Zero);
                    Assert.AreEqual(true, contractPermission.IsAllowed(contractManifest, "AAA"));
                    contractPermission.Contract = ContractPermissionDescriptor.CreateWildcard();
                }
                else if (i == 1)
                {
                    ContractManifest contractManifest = ContractManifest.CreateDefault(UInt160.Zero);
                    ContractPermission contractPermission = ContractPermission.DefaultPermission;
                    contractPermission.Contract = ContractPermissionDescriptor.Create(UInt160.Parse("0x0000000000000000000000000000000000000001"));
                    Assert.AreEqual(false, contractPermission.IsAllowed(contractManifest, "AAA"));
                    contractPermission.Contract = ContractPermissionDescriptor.CreateWildcard();
                }
                else if (i == 2)
                {
                    Random random = new Random();
                    byte[] privateKey = new byte[32];
                    random.NextBytes(privateKey);
                    ECPoint publicKey = ECCurve.Secp256r1.G * privateKey;
                    ContractManifest contractManifest = ContractManifest.CreateDefault(UInt160.Zero);
                    contractManifest.Groups = new ContractGroup[] { new ContractGroup() { PubKey = publicKey } };
                    ContractPermission contractPermission = ContractPermission.DefaultPermission;
                    contractPermission.Contract = ContractPermissionDescriptor.Create(publicKey);
                    Assert.AreEqual(true, contractPermission.IsAllowed(contractManifest, "AAA"));
                    contractPermission.Contract = ContractPermissionDescriptor.CreateWildcard();
                }
                else
                {
                    Random random = new Random();
                    byte[] privateKey = new byte[32];
                    random.NextBytes(privateKey);
                    ECPoint publicKey = ECCurve.Secp256r1.G * privateKey;
                    byte[] privateKey2 = new byte[32];
                    random.NextBytes(privateKey2);
                    ECPoint publicKey2 = ECCurve.Secp256r1.G * privateKey2;
                    ContractManifest contractManifest = ContractManifest.CreateDefault(UInt160.Zero);
                    contractManifest.Groups = new ContractGroup[] { new ContractGroup() { PubKey = publicKey2 } };
                    ContractPermission contractPermission = ContractPermission.DefaultPermission;
                    contractPermission.Contract = ContractPermissionDescriptor.Create(publicKey);
                    Assert.AreEqual(false, contractPermission.IsAllowed(contractManifest, "AAA"));
                    contractPermission.Contract = ContractPermissionDescriptor.CreateWildcard();
                }
            }
        }
    }
}
