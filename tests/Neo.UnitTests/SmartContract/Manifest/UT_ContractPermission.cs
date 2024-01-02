// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ContractPermission.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractPermission
    {
        [TestMethod]
        public void TestDeserialize()
        {
            // null
            ContractPermission contractPermission = ContractPermission.DefaultPermission;
            Struct s = (Struct)contractPermission.ToStackItem(new VM.ReferenceCounter());

            contractPermission = s.ToInteroperable<ContractPermission>();
            Assert.IsTrue(contractPermission.Contract.IsWildcard);
            Assert.IsTrue(contractPermission.Methods.IsWildcard);

            // not null
            contractPermission = new ContractPermission()
            {
                Contract = ContractPermissionDescriptor.Create(UInt160.Zero),
                Methods = WildcardContainer<string>.Create("test")
            };
            s = (Struct)contractPermission.ToStackItem(new VM.ReferenceCounter());

            contractPermission = s.ToInteroperable<ContractPermission>();
            Assert.IsFalse(contractPermission.Contract.IsWildcard);
            Assert.IsFalse(contractPermission.Methods.IsWildcard);
            Assert.AreEqual(UInt160.Zero, contractPermission.Contract.Hash);
            Assert.AreEqual("test", contractPermission.Methods[0]);
        }

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

            Random random = new();
            byte[] privateKey3 = new byte[32];
            random.NextBytes(privateKey3);
            ECPoint publicKey3 = ECCurve.Secp256r1.G * privateKey3;
            ContractManifest contractManifest3 = TestUtils.CreateDefaultManifest();
            contractManifest3.Groups = new ContractGroup[] { new ContractGroup() { PubKey = publicKey3 } };
            ContractPermission contractPermission3 = ContractPermission.DefaultPermission;
            contractPermission3.Contract = ContractPermissionDescriptor.Create(publicKey3);
            Assert.AreEqual(true, contractPermission3.IsAllowed(new ContractState() { Hash = UInt160.Zero, Manifest = contractManifest3 }, "AAA"));
            contractPermission3.Contract = ContractPermissionDescriptor.CreateWildcard();

            byte[] privateKey41 = new byte[32];
            random.NextBytes(privateKey41);
            ECPoint publicKey41 = ECCurve.Secp256r1.G * privateKey41;
            byte[] privateKey42 = new byte[32];
            random.NextBytes(privateKey42);
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
