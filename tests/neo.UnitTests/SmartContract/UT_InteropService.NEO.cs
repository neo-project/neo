using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;
using System.Text;
using VMArray = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_InteropService
    {
        [TestMethod]
        public void TestCheckSig()
        {
            var engine = GetEngine(true);
            IVerifiable iv = engine.ScriptContainer;
            byte[] message = iv.GetHashData();
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            ECPoint pubkey = keyPair.PublicKey;
            byte[] signature = Crypto.Sign(message, privateKey, pubkey.EncodePoint(false).Skip(1).ToArray());
            engine.VerifyWithECDsaSecp256r1(StackItem.Null, pubkey.EncodePoint(false), signature).Should().BeTrue();
            engine.VerifyWithECDsaSecp256r1(StackItem.Null, new byte[70], signature).Should().BeFalse();
        }

        [TestMethod]
        public void TestCrypto_CheckMultiSig()
        {
            var engine = GetEngine(true);
            IVerifiable iv = engine.ScriptContainer;
            byte[] message = iv.GetHashData();

            byte[] privkey1 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair key1 = new KeyPair(privkey1);
            ECPoint pubkey1 = key1.PublicKey;
            byte[] signature1 = Crypto.Sign(message, privkey1, pubkey1.EncodePoint(false).Skip(1).ToArray());

            byte[] privkey2 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02};
            KeyPair key2 = new KeyPair(privkey2);
            ECPoint pubkey2 = key2.PublicKey;
            byte[] signature2 = Crypto.Sign(message, privkey2, pubkey2.EncodePoint(false).Skip(1).ToArray());

            var pubkeys = new[]
            {
                pubkey1.EncodePoint(false),
                pubkey2.EncodePoint(false)
            };
            var signatures = new[]
            {
                signature1,
                signature2
            };
            engine.CheckMultisigWithECDsaSecp256r1(StackItem.Null, pubkeys, signatures).Should().BeTrue();

            pubkeys = new byte[0][];
            Assert.ThrowsException<ArgumentException>(() => engine.CheckMultisigWithECDsaSecp256r1(StackItem.Null, pubkeys, signatures));

            pubkeys = new[]
            {
                pubkey1.EncodePoint(false),
                pubkey2.EncodePoint(false)
            };
            signatures = new byte[0][];
            Assert.ThrowsException<ArgumentException>(() => engine.CheckMultisigWithECDsaSecp256r1(StackItem.Null, pubkeys, signatures));

            pubkeys = new[]
            {
                pubkey1.EncodePoint(false),
                pubkey2.EncodePoint(false)
            };
            signatures = new[]
            {
                signature1,
                new byte[64]
            };
            engine.CheckMultisigWithECDsaSecp256r1(StackItem.Null, pubkeys, signatures).Should().BeFalse();

            pubkeys = new[]
            {
                pubkey1.EncodePoint(false),
                new byte[70]
            };
            signatures = new[]
            {
                signature1,
                signature2
            };
            engine.CheckMultisigWithECDsaSecp256r1(StackItem.Null, pubkeys, signatures).Should().BeFalse();
        }

        [TestMethod]
        public void TestAccount_IsStandard()
        {
            var engine = GetEngine(false, true);
            var hash = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01,
                                    0x01, 0x01, 0x01, 0x01, 0x01,
                                    0x01, 0x01, 0x01, 0x01, 0x01,
                                    0x01, 0x01, 0x01, 0x01, 0x01 };
            engine.IsStandardContract(new UInt160(hash)).Should().BeFalse();

            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();
            snapshot.AddContract(state.Hash, state);
            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });
            engine.IsStandardContract(state.Hash).Should().BeFalse();

            state.Script = Contract.CreateSignatureRedeemScript(Blockchain.StandbyValidators[0]);
            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });
            engine.IsStandardContract(state.Hash).Should().BeTrue();
        }

        [TestMethod]
        public void TestContract_Create()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { };
            var nef = new NefFile()
            {
                Script = new byte[byte.MaxValue],
                Compiler = "",
                Version = new Version(1, 2, 3, 4).ToString()
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            var nefFile = nef.ToArray();
            var manifest = TestUtils.CreateDefaultManifest();
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(null, nefFile, manifest.ToJson().ToByteArray(false)));
            Assert.ThrowsException<ArgumentException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, new byte[ContractManifest.MaxLength + 1]));
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(true), 10000000));

            var script_exceedMaxLength = new NefFile()
            {
                Script = new byte[NefFile.MaxScriptLength - 1],
                Compiler = "",
                Version = new Version(1, 2, 3, 4).ToString()
            };
            script_exceedMaxLength.CheckSum = NefFile.ComputeChecksum(nef);
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, script_exceedMaxLength.ToArray(), manifest.ToJson().ToByteArray(true)));

            var script_zeroLength = new byte[] { };
            Assert.ThrowsException<ArgumentException>(() => snapshot.DeployContract(UInt160.Zero, script_zeroLength, manifest.ToJson().ToByteArray(true)));

            var manifest_zeroLength = new byte[] { };
            Assert.ThrowsException<ArgumentException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest_zeroLength));

            manifest = TestUtils.CreateDefaultManifest();
            var ret = snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false));
            ret.Hash.ToString().Should().Be("0x5756874a149b9de89c7b5d34f9c37db3762f88a2");
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false)));

            var state = TestUtils.GetContract();
            snapshot.AddContract(state.Hash, state);

            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false)));
        }

        [TestMethod]
        public void TestContract_Update()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { };

            var nef = new NefFile()
            {
                Script = new byte[] { 0x01 },
                Compiler = "",
                Version = new Version(1, 2, 3, 4).ToString()
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, nef.ToArray(), new byte[0]));

            var manifest = TestUtils.CreateDefaultManifest();
            byte[] privkey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair key = new KeyPair(privkey);
            ECPoint pubkey = key.PublicKey;
            var state = TestUtils.GetContract();
            byte[] signature = Crypto.Sign(state.Hash.ToArray(), privkey, pubkey.EncodePoint(false).Skip(1).ToArray());
            manifest.Groups = new ContractGroup[]
            {
                new ContractGroup()
                {
                    PubKey = pubkey,
                    Signature = signature
                }
            };

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01 },
                IsConstant = false
            };

            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };
            snapshot.AddContract(state.Hash, state);
            snapshot.Storages.Add(storageKey, storageItem);
            state.UpdateCounter.Should().Be(0);
            snapshot.UpdateContract(state.Hash, nef.ToArray(), manifest.ToJson().ToByteArray(false));
            var ret = NativeContract.ContractManagement.GetContract(snapshot, state.Hash);
            snapshot.Storages.Find(BitConverter.GetBytes(state.Id)).ToList().Count().Should().Be(1);
            ret.UpdateCounter.Should().Be(1);
            ret.Id.Should().Be(state.Id);
            ret.Manifest.ToJson().ToString().Should().Be(manifest.ToJson().ToString());
            ret.Script.ToHexString().Should().Be(nef.Script.ToHexString().ToString());
        }

        [TestMethod]
        public void TestContract_Update_Invalid()
        {
            var nefFile = new NefFile()
            {
                Script = new byte[] { 0x01 },
                Version = new Version(1, 2, 3, 4).ToString(),
                Compiler = ""
            };
            nefFile.CheckSum = NefFile.ComputeChecksum(nefFile);

            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block();

            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, null, new byte[] { 0x01 }));
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, nefFile.ToArray(), null));
            Assert.ThrowsException<ArgumentException>(() => snapshot.UpdateContract(null, null, null));

            nefFile = new NefFile()
            {
                Script = new byte[0],
                Version = new Version(1, 2, 3, 4).ToString(),
                Compiler = ""
            };
            nefFile.CheckSum = NefFile.ComputeChecksum(nefFile);

            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, nefFile.ToArray(), new byte[] { 0x01 }));
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, nefFile.ToArray(), new byte[0]));
        }

        [TestMethod]
        public void TestStorage_Find()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var state = TestUtils.GetContract();

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = true
            };
            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };
            snapshot.AddContract(state.Hash, state);
            snapshot.Storages.Add(storageKey, storageItem);
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });

            var iterator = engine.Find(new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            }, new byte[] { 0x01 });
            iterator.Next();
            var ele = iterator.Value();
            ele.GetSpan().ToHexString().Should().Be(storageItem.Value.ToHexString());
        }

        [TestMethod]
        public void TestEnumerator_Create()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var ret = engine.CreateEnumerator(arr);
            ret.Next();
            ret.Value().GetSpan().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());
        }

        [TestMethod]
        public void TestEnumerator_Next()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            engine.EnumeratorNext(new ArrayWrapper(arr)).Should().BeTrue();
        }

        [TestMethod]
        public void TestEnumerator_Value()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var wrapper = new ArrayWrapper(arr);
            wrapper.Next();
            engine.EnumeratorValue(wrapper).GetSpan().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());
        }

        [TestMethod]
        public void TestIterator_Create()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var ret = engine.CreateIterator(arr);
            ret.Next();
            ret.Value().GetSpan().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());

            var interop = new InteropInterface(1);
            Assert.ThrowsException<ArgumentException>(() => engine.CreateIterator(interop));

            var map = new Map
            {
                [1] = 2,
                [3] = 4
            };
            ret = engine.CreateIterator(map);
            ret.Next();
            ret.Key().GetInteger().Should().Be(1);
            ret.Value().GetInteger().Should().Be(2);
        }

        [TestMethod]
        public void TestIterator_Key()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var wrapper = new ArrayWrapper(arr);
            wrapper.Next();
            engine.IteratorKey(wrapper).GetInteger().Should().Be(0);
        }

        [TestMethod]
        public void TestIterator_Keys()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var wrapper = new ArrayWrapper(arr);
            var ret = engine.IteratorKeys(wrapper);
            ret.Next();
            ret.Value().GetInteger().Should().Be(0);
        }

        [TestMethod]
        public void TestIterator_Values()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var wrapper = new ArrayWrapper(arr);
            var ret = engine.IteratorValues(wrapper);
            ret.Next();
            ret.Value().GetSpan().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());
        }

        [TestMethod]
        public void TestJson_Deserialize()
        {
            GetEngine().JsonDeserialize(new byte[] { (byte)'1' }).GetInteger().Should().Be(1);
        }

        [TestMethod]
        public void TestJson_Serialize()
        {
            Encoding.UTF8.GetString(GetEngine().JsonSerialize(1)).Should().Be("1");
        }
    }
}
