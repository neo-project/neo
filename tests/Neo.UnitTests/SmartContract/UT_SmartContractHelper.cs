// Copyright (C) 2015-2024 The Neo Project.
//
// UT_SmartContractHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.Wallets;
using System;
using System.Linq;
using System.Security.Cryptography;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_SmartContractHelper
    {

        [TestMethod]
        public void TestIsMultiSigContract()
        {
            ECPoint[] publicKeys1 = new ECPoint[20];
            for (int i = 0; i < 20; i++)
            {
                byte[] privateKey1 = new byte[32];
                RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
                rng1.GetBytes(privateKey1);
                KeyPair key1 = new(privateKey1);
                publicKeys1[i] = key1.PublicKey;
            }
            byte[] script1 = Contract.CreateMultiSigRedeemScript(20, publicKeys1);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script1, out _, out ECPoint[] p1));
            CollectionAssert.AreEqual(publicKeys1.OrderBy(p => p).ToArray(), p1);

            ECPoint[] publicKeys2 = new ECPoint[256];
            for (int i = 0; i < 256; i++)
            {
                byte[] privateKey2 = new byte[32];
                RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
                rng2.GetBytes(privateKey2);
                KeyPair key2 = new(privateKey2);
                publicKeys2[i] = key2.PublicKey;
            }
            byte[] script2 = Contract.CreateMultiSigRedeemScript(256, publicKeys2);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script2, out _, out ECPoint[] p2));
            CollectionAssert.AreEqual(publicKeys2.OrderBy(p => p).ToArray(), p2);

            ECPoint[] publicKeys3 = new ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey3 = new byte[32];
                RandomNumberGenerator rng3 = RandomNumberGenerator.Create();
                rng3.GetBytes(privateKey3);
                KeyPair key3 = new(privateKey3);
                publicKeys3[i] = key3.PublicKey;
            }
            byte[] script3 = Contract.CreateMultiSigRedeemScript(3, publicKeys3);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script3, out _, out ECPoint[] p3));
            CollectionAssert.AreEqual(publicKeys3.OrderBy(p => p).ToArray(), p3);

            ECPoint[] publicKeys4 = new ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey4 = new byte[32];
                RandomNumberGenerator rng4 = RandomNumberGenerator.Create();
                rng4.GetBytes(privateKey4);
                KeyPair key4 = new(privateKey4);
                publicKeys4[i] = key4.PublicKey;
            }
            byte[] script4 = Contract.CreateMultiSigRedeemScript(3, publicKeys4);
            script4[^1] = 0x00;
            Assert.AreEqual(false, Neo.SmartContract.Helper.IsMultiSigContract(script4, out _, out ECPoint[] p4));
            Assert.IsNull(p4);
        }

        [TestMethod]
        public void TestIsSignatureContract()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new(privateKey);
            byte[] script = Contract.CreateSignatureRedeemScript(key.PublicKey);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsSignatureContract(script));
            script[0] = 0x22;
            Assert.AreEqual(false, Neo.SmartContract.Helper.IsSignatureContract(script));
        }

        [TestMethod]
        public void TestIsStandardContract()
        {
            byte[] privateKey1 = new byte[32];
            RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new(privateKey1);
            byte[] script1 = Contract.CreateSignatureRedeemScript(key1.PublicKey);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsStandardContract(script1));

            ECPoint[] publicKeys2 = new ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey2 = new byte[32];
                RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
                rng2.GetBytes(privateKey2);
                KeyPair key2 = new(privateKey2);
                publicKeys2[i] = key2.PublicKey;
            }
            byte[] script2 = Contract.CreateMultiSigRedeemScript(3, publicKeys2);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsStandardContract(script2));
        }

        [TestMethod]
        public void TestVerifyWitnesses()
        {
            var snapshotCache1 = TestBlockchain.GetTestSnapshotCache().CreateSnapshot();
            UInt256 index1 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TestUtils.BlocksAdd(snapshotCache1, index1, new TrimmedBlock()
            {
                Header = new Header
                {
                    Timestamp = 1,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Hashes = new UInt256[1] { UInt256.Zero },
            });
            TestUtils.BlocksDelete(snapshotCache1, index1);
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(new Header() { PrevHash = index1 }, TestProtocolSettings.Default, snapshotCache1, 100));

            var snapshotCache2 = TestBlockchain.GetTestSnapshotCache();
            UInt256 index2 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TrimmedBlock block2 = new()
            {
                Header = new Header
                {
                    Timestamp = 2,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Hashes = new UInt256[1] { UInt256.Zero },
            };
            TestUtils.BlocksAdd(snapshotCache2, index2, block2);
            Header header2 = new() { PrevHash = index2, Witness = new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() } };

            snapshotCache2.AddContract(UInt160.Zero, new ContractState());
            snapshotCache2.DeleteContract(UInt160.Zero);
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(header2, TestProtocolSettings.Default, snapshotCache2, 100));

            var snapshotCache3 = TestBlockchain.GetTestSnapshotCache();
            UInt256 index3 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TrimmedBlock block3 = new()
            {
                Header = new Header
                {
                    Timestamp = 3,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Hashes = new UInt256[1] { UInt256.Zero },
            };
            TestUtils.BlocksAdd(snapshotCache3, index3, block3);
            Header header3 = new()
            {
                PrevHash = index3,
                Witness = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            };
            snapshotCache3.AddContract(UInt160.Zero, new ContractState()
            {
                Nef = new NefFile { Script = Array.Empty<byte>() },
                Hash = Array.Empty<byte>().ToScriptHash(),
                Manifest = TestUtils.CreateManifest("verify", ContractParameterType.Boolean, ContractParameterType.Signature),
            });
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(header3, TestProtocolSettings.Default, snapshotCache3, 100));

            // Smart contract verification

            var contract = new ContractState()
            {
                Nef = new NefFile { Script = "11".HexToBytes() }, // 17 PUSH1
                Hash = "11".HexToBytes().ToScriptHash(),
                Manifest = TestUtils.CreateManifest("verify", ContractParameterType.Boolean, ContractParameterType.Signature), // Offset = 0
            };
            snapshotCache3.AddContract(contract.Hash, contract);
            var tx = new Nep17NativeContractExtensions.ManualWitness(contract.Hash)
            {
                Witnesses = new Witness[] { new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() } }
            };

            Assert.AreEqual(true, Neo.SmartContract.Helper.VerifyWitnesses(tx, TestProtocolSettings.Default, snapshotCache3, 1000));
        }
    }
}
