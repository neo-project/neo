using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
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
        const byte Prefix_Block = 5;
        const byte Prefix_BlockHash = 9;
        const byte Prefix_Transaction = 11;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestIsMultiSigContract()
        {
            ECPoint[] publicKeys1 = new ECPoint[20];
            for (int i = 0; i < 20; i++)
            {
                byte[] privateKey1 = new byte[32];
                RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
                rng1.GetBytes(privateKey1);
                KeyPair key1 = new KeyPair(privateKey1);
                publicKeys1[i] = key1.PublicKey;
            }
            byte[] script1 = Contract.CreateMultiSigRedeemScript(20, publicKeys1);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script1, out _, out ECPoint[] p1));
            CollectionAssert.AreEqual(publicKeys1.OrderBy(p => p).ToArray(), p1);

            Neo.Cryptography.ECC.ECPoint[] publicKeys2 = new Neo.Cryptography.ECC.ECPoint[256];
            for (int i = 0; i < 256; i++)
            {
                byte[] privateKey2 = new byte[32];
                RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
                rng2.GetBytes(privateKey2);
                KeyPair key2 = new KeyPair(privateKey2);
                publicKeys2[i] = key2.PublicKey;
            }
            byte[] script2 = Contract.CreateMultiSigRedeemScript(256, publicKeys2);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script2, out _, out ECPoint[] p2));
            CollectionAssert.AreEqual(publicKeys2.OrderBy(p => p).ToArray(), p2);

            Neo.Cryptography.ECC.ECPoint[] publicKeys3 = new Neo.Cryptography.ECC.ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey3 = new byte[32];
                RandomNumberGenerator rng3 = RandomNumberGenerator.Create();
                rng3.GetBytes(privateKey3);
                KeyPair key3 = new KeyPair(privateKey3);
                publicKeys3[i] = key3.PublicKey;
            }
            byte[] script3 = Contract.CreateMultiSigRedeemScript(3, publicKeys3);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script3, out _, out ECPoint[] p3));
            CollectionAssert.AreEqual(publicKeys3.OrderBy(p => p).ToArray(), p3);

            Neo.Cryptography.ECC.ECPoint[] publicKeys4 = new Neo.Cryptography.ECC.ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey4 = new byte[32];
                RandomNumberGenerator rng4 = RandomNumberGenerator.Create();
                rng4.GetBytes(privateKey4);
                KeyPair key4 = new KeyPair(privateKey4);
                publicKeys4[i] = key4.PublicKey;
            }
            byte[] script4 = Contract.CreateMultiSigRedeemScript(3, publicKeys4);
            script4[script4.Length - 1] = 0x00;
            Assert.AreEqual(false, Neo.SmartContract.Helper.IsMultiSigContract(script4, out _, out ECPoint[] p4));
            Assert.IsNull(p4);
        }

        [TestMethod]
        public void TestIsSignatureContract()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
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
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] script1 = Contract.CreateSignatureRedeemScript(key1.PublicKey);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsStandardContract(script1));

            Neo.Cryptography.ECC.ECPoint[] publicKeys2 = new Neo.Cryptography.ECC.ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey2 = new byte[32];
                RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
                rng2.GetBytes(privateKey2);
                KeyPair key2 = new KeyPair(privateKey2);
                publicKeys2[i] = key2.PublicKey;
            }
            byte[] script2 = Contract.CreateMultiSigRedeemScript(3, publicKeys2);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsStandardContract(script2));
        }

        [TestMethod]
        public void TestVerifyWitnesses()
        {
            var snapshot1 = TestBlockchain.GetTestSnapshot().CreateSnapshot();
            UInt256 index1 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            BlocksAdd(snapshot1, index1, new TrimmedBlock()
            {
                Header = new Header
                {
                    Timestamp = 1,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness() { InvocationScript = new byte[0], VerificationScript = new byte[0] }
                },
                Hashes = new UInt256[1] { UInt256.Zero },
            });
            BlocksDelete(snapshot1, index1);
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(new Header() { PrevHash = index1 }, snapshot1, 100));

            var snapshot2 = TestBlockchain.GetTestSnapshot();
            UInt256 index2 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TrimmedBlock block2 = new TrimmedBlock()
            {
                Header = new Header
                {
                    Timestamp = 2,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness() { InvocationScript = new byte[0], VerificationScript = new byte[0] }
                },
                Hashes = new UInt256[1] { UInt256.Zero },
            };
            BlocksAdd(snapshot2, index2, block2);
            Header header2 = new Header() { PrevHash = index2, Witness = new Witness { InvocationScript = new byte[0], VerificationScript = new byte[0] } };

            snapshot2.AddContract(UInt160.Zero, new ContractState());
            snapshot2.DeleteContract(UInt160.Zero);
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(header2, snapshot2, 100));

            var snapshot3 = TestBlockchain.GetTestSnapshot();
            UInt256 index3 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TrimmedBlock block3 = new TrimmedBlock()
            {
                Header = new Header
                {
                    Timestamp = 3,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness() { InvocationScript = new byte[0], VerificationScript = new byte[0] }
                },
                Hashes = new UInt256[1] { UInt256.Zero },
            };
            BlocksAdd(snapshot3, index3, block3);
            Header header3 = new Header()
            {
                PrevHash = index3,
                Witness = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            };
            snapshot3.AddContract(UInt160.Zero, new ContractState()
            {
                Nef = new NefFile { Script = Array.Empty<byte>() },
                Hash = Array.Empty<byte>().ToScriptHash(),
                Manifest = TestUtils.CreateManifest("verify", ContractParameterType.Boolean, ContractParameterType.Signature),
            });
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(header3, snapshot3, 100));

            // Smart contract verification

            var contract = new ContractState()
            {
                Nef = new NefFile { Script = "11".HexToBytes() }, // 17 PUSH1
                Hash = "11".HexToBytes().ToScriptHash(),
                Manifest = TestUtils.CreateManifest("verify", ContractParameterType.Boolean, ContractParameterType.Signature), // Offset = 0
            };
            snapshot3.AddContract(contract.Hash, contract);
            var tx = new Nep17NativeContractExtensions.ManualWitness(contract.Hash)
            {
                Witnesses = new Witness[] { new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() } }
            };

            Assert.AreEqual(true, Neo.SmartContract.Helper.VerifyWitnesses(tx, snapshot3, 1000));
        }

        private void BlocksDelete(DataCache snapshot, UInt256 hash)
        {
            snapshot.Delete(NativeContract.Ledger.CreateStorageKey(Prefix_BlockHash, hash));
            snapshot.Delete(NativeContract.Ledger.CreateStorageKey(Prefix_Block, hash));
        }

        public static void TransactionAdd(DataCache snapshot, params TransactionState[] txs)
        {
            foreach (TransactionState tx in txs)
            {
                snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_Transaction, tx.Transaction.Hash), new StorageItem(tx, true));
            }
        }

        public static void BlocksAdd(DataCache snapshot, UInt256 hash, TrimmedBlock block)
        {
            snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_BlockHash, block.Index), new StorageItem(hash.ToArray(), true));
            snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_Block, hash), new StorageItem(block.ToArray(), true));
        }
    }
}
