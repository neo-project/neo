using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using Neo.Wallets;
using System.Linq;
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
            byte[] signature = Crypto.Default.Sign(message, privateKey, pubkey.EncodePoint(false).Skip(1).ToArray());
            engine.CurrentContext.EvaluationStack.Push(signature);
            engine.CurrentContext.EvaluationStack.Push(pubkey.EncodePoint(false));
            InteropService.Invoke(engine, InteropService.Neo_Crypto_CheckSig).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeTrue();

            engine.CurrentContext.EvaluationStack.Push(signature);
            engine.CurrentContext.EvaluationStack.Push(new byte[70]);
            InteropService.Invoke(engine, InteropService.Neo_Crypto_CheckSig).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeFalse();
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
            byte[] signature1 = Crypto.Default.Sign(message, privkey1, pubkey1.EncodePoint(false).Skip(1).ToArray());

            byte[] privkey2 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02};
            KeyPair key2 = new KeyPair(privkey2);
            ECPoint pubkey2 = key2.PublicKey;
            byte[] signature2 = Crypto.Default.Sign(message, privkey2, pubkey2.EncodePoint(false).Skip(1).ToArray());

            var pubkeys = new VMArray
            {
                pubkey1.EncodePoint(false),
                pubkey2.EncodePoint(false)
            };
            var signatures = new VMArray
            {
                signature1,
                signature2
            };
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            InteropService.Invoke(engine, InteropService.Neo_Crypto_CheckMultiSig).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeTrue();

            pubkeys = new VMArray();
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            InteropService.Invoke(engine, InteropService.Neo_Crypto_CheckMultiSig).Should().BeFalse();

            pubkeys = new VMArray
            {
                pubkey1.EncodePoint(false),
                pubkey2.EncodePoint(false)
            };
            signatures = new VMArray();
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            InteropService.Invoke(engine, InteropService.Neo_Crypto_CheckMultiSig).Should().BeFalse();

            pubkeys = new VMArray
            {
                pubkey1.EncodePoint(false),
                pubkey2.EncodePoint(false)
            };
            signatures = new VMArray
            {
                signature1,
                new byte[64]
            };
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            InteropService.Invoke(engine, InteropService.Neo_Crypto_CheckMultiSig).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeFalse();

            pubkeys = new VMArray
            {
                pubkey1.EncodePoint(false),
                new byte[70]
            };
            signatures = new VMArray
            {
                signature1,
                signature2
            };
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            InteropService.Invoke(engine, InteropService.Neo_Crypto_CheckMultiSig).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestAccount_IsStandard()
        {
            var engine = GetEngine(false, true);
            var hash = new byte[] { 0x01, 0x01, 0x01 ,0x01, 0x01,
                                    0x01, 0x01, 0x01, 0x01, 0x01,
                                    0x01, 0x01, 0x01, 0x01, 0x01,
                                    0x01, 0x01, 0x01, 0x01, 0x01 };
            engine.CurrentContext.EvaluationStack.Push(hash);
            InteropService.Invoke(engine, InteropService.Neo_Account_IsStandard).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeTrue();

            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(state.ScriptHash.ToArray());
            InteropService.Invoke(engine, InteropService.Neo_Account_IsStandard).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void TestContract_Create()
        {
            var engine = GetEngine(false, true);
            var script = new byte[1024 * 1024 + 1];
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Create).Should().BeFalse();

            string manifestStr = new string(new char[ContractManifest.MaxLength + 1]);
            script = new byte[] { 0x01 };
            engine.CurrentContext.EvaluationStack.Push(manifestStr);
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Create).Should().BeFalse();

            var manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"));
            engine.CurrentContext.EvaluationStack.Push(manifest.ToString());
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Create).Should().BeFalse();

            manifest.Abi.Hash = script.ToScriptHash();
            engine.CurrentContext.EvaluationStack.Push(manifest.ToString());
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Create).Should().BeTrue();

            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(manifest.ToString());
            engine.CurrentContext.EvaluationStack.Push(state.Script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Create).Should().BeFalse();
        }

        [TestMethod]
        public void TestContract_Update()
        {
            var engine = GetEngine(false, true);
            var script = new byte[1024 * 1024 + 1];
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Update).Should().BeFalse();

            string manifestStr = new string(new char[ContractManifest.MaxLength + 1]);
            script = new byte[] { 0x01 };
            engine.CurrentContext.EvaluationStack.Push(manifestStr);
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Update).Should().BeFalse();

            manifestStr = "";
            engine.CurrentContext.EvaluationStack.Push(manifestStr);
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Update).Should().BeFalse();

            var manifest = ContractManifest.CreateDefault(script.ToScriptHash());
            byte[] privkey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair key = new KeyPair(privkey);
            ECPoint pubkey = key.PublicKey;
            byte[] signature = Crypto.Default.Sign(script.ToScriptHash().ToArray(), privkey, pubkey.EncodePoint(false).Skip(1).ToArray());
            manifest.Groups = new ContractGroup[]
            {
                new ContractGroup()
                {
                    PubKey = pubkey,
                    Signature = signature
                }
            };
            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01 },
                IsConstant = false
            };

            var storageKey = new StorageKey
            {
                ScriptHash = state.ScriptHash,
                Key = new byte[] { 0x01 }
            };
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>(storageKey, storageItem));
            engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(state.Script);
            engine.CurrentContext.EvaluationStack.Push(manifest.ToString());
            engine.CurrentContext.EvaluationStack.Push(script);
            InteropService.Invoke(engine, InteropService.Neo_Contract_Update).Should().BeTrue();
        }

        [TestMethod]
        public void TestStorage_Find()
        {
            var mockSnapshot = new Mock<Snapshot>();
            var state = TestUtils.GetContract();
            state.Manifest.Features = ContractFeatures.HasStorage;

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = true
            };
            var storageKey = new StorageKey
            {
                ScriptHash = state.ScriptHash,
                Key = new byte[] { 0x01 }
            };
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>(state.ScriptHash, state));
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>(storageKey, storageItem));
            var engine = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            engine.LoadScript(new byte[] { 0x01 });

            engine.CurrentContext.EvaluationStack.Push(new byte[] { 0x01 });
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<StorageContext>(new StorageContext
            {
                ScriptHash = state.ScriptHash,
                IsReadOnly = false
            }));
            InteropService.Invoke(engine, InteropService.Neo_Storage_Find).Should().BeTrue();
            var iterator = ((InteropInterface<StorageIterator>)engine.CurrentContext.EvaluationStack.Pop()).GetInterface<StorageIterator>();
            iterator.Next();
            var ele = iterator.Value();
            ele.GetByteArray().ToHexString().Should().Be(storageItem.Value.ToHexString());

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Storage_Find).Should().BeFalse();
        }

        [TestMethod]
        public void TestEnumerator_Create()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            engine.CurrentContext.EvaluationStack.Push(arr);
            InteropService.Invoke(engine, InteropService.Neo_Enumerator_Create).Should().BeTrue();
            var ret = (InteropInterface<IEnumerator>)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<IEnumerator>().Next();
            ret.GetInterface<IEnumerator>().Value().GetByteArray().ToHexString()
                .Should().Be(new byte[] { 0x01 }.ToHexString());

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Enumerator_Create).Should().BeFalse();
        }

        [TestMethod]
        public void TestEnumerator_Next()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(new ArrayWrapper(arr)));
            InteropService.Invoke(engine, InteropService.Neo_Enumerator_Next).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBoolean().Should().BeTrue();

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Enumerator_Next).Should().BeFalse();
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
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper));
            InteropService.Invoke(engine, InteropService.Neo_Enumerator_Value).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetByteArray().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Enumerator_Value).Should().BeFalse();
        }

        [TestMethod]
        public void TestEnumerator_Concat()
        {
            var engine = GetEngine();
            var arr1 = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var arr2 = new VMArray {
                new byte[]{ 0x03 },
                new byte[]{ 0x04 }
            };
            var wrapper1 = new ArrayWrapper(arr1);
            var wrapper2 = new ArrayWrapper(arr2);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper2));
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper1));
            InteropService.Invoke(engine, InteropService.Neo_Enumerator_Concat).Should().BeTrue();
            var ret = ((InteropInterface<IEnumerator>)engine.CurrentContext.EvaluationStack.Pop()).GetInterface<IEnumerator>();
            ret.Next().Should().BeTrue();
            ret.Value().GetByteArray().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());
        }

        [TestMethod]
        public void TestIterator_Create()
        {
            var engine = GetEngine();
            var arr = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            engine.CurrentContext.EvaluationStack.Push(arr);
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Create).Should().BeTrue();
            var ret = (InteropInterface<IIterator>)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<IIterator>().Next();
            ret.GetInterface<IIterator>().Value().GetByteArray().ToHexString()
                .Should().Be(new byte[] { 0x01 }.ToHexString());

            var map = new Map
            {
                { new Integer(1), new Integer(2) },
                { new Integer(3), new Integer(4) }
            };
            engine.CurrentContext.EvaluationStack.Push(map);
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Create).Should().BeTrue();
            ret = (InteropInterface<IIterator>)engine.CurrentContext.EvaluationStack.Pop();
            ret.GetInterface<IIterator>().Next();
            ret.GetInterface<IIterator>().Key().GetBigInteger().Should().Be(1);
            ret.GetInterface<IIterator>().Value().GetBigInteger().Should().Be(2);

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Create).Should().BeFalse();
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
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper));
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Key).Should().BeTrue();
            engine.CurrentContext.EvaluationStack.Pop().GetBigInteger().Should().Be(0);

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Key).Should().BeFalse();
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
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper));
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Keys).Should().BeTrue();
            var ret = ((InteropInterface<IteratorKeysWrapper>)engine.CurrentContext.EvaluationStack.Pop()).GetInterface<IteratorKeysWrapper>();
            ret.Next();
            ret.Value().GetBigInteger().Should().Be(0);

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Keys).Should().BeFalse();
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
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper));
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Values).Should().BeTrue();
            var ret = ((InteropInterface<IteratorValuesWrapper>)engine.CurrentContext.EvaluationStack.Pop()).GetInterface<IteratorValuesWrapper>();
            ret.Next();
            ret.Value().GetByteArray().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());

            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Values).Should().BeFalse();
        }

        [TestMethod]
        public void TestIterator_Concat()
        {
            var engine = GetEngine();
            var arr1 = new VMArray {
                new byte[]{ 0x01 },
                new byte[]{ 0x02 }
            };
            var arr2 = new VMArray {
                new byte[]{ 0x03 },
                new byte[]{ 0x04 }
            };
            var wrapper1 = new ArrayWrapper(arr1);
            var wrapper2 = new ArrayWrapper(arr2);
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper2));
            engine.CurrentContext.EvaluationStack.Push(new InteropInterface<ArrayWrapper>(wrapper1));
            InteropService.Invoke(engine, InteropService.Neo_Iterator_Concat).Should().BeTrue();
            var ret = ((InteropInterface<IIterator>)engine.CurrentContext.EvaluationStack.Pop()).GetInterface<IIterator>();
            ret.Next().Should().BeTrue();
            ret.Value().GetByteArray().ToHexString().Should().Be(new byte[] { 0x01 }.ToHexString());
        }

        [TestMethod]
        public void TestJson_Deserialize()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push("1");
            InteropService.Invoke(engine, InteropService.Neo_Json_Deserialize).Should().BeTrue();
            var ret = engine.CurrentContext.EvaluationStack.Pop();
            ret.GetBigInteger().Should().Be(1);
        }

        [TestMethod]
        public void TestJson_Serialize()
        {
            var engine = GetEngine();
            engine.CurrentContext.EvaluationStack.Push(1);
            InteropService.Invoke(engine, InteropService.Neo_Json_Serialize).Should().BeTrue();
            var ret = engine.CurrentContext.EvaluationStack.Pop();
            ret.GetString().Should().Be("1");
        }
    }
}
