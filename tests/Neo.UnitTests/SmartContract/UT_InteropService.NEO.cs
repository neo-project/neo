using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_InteropService
    {
        [TestMethod]
        public void TestCheckSig()
        {
            var engine = GetEngine(true);
            IVerifiable iv = engine.ScriptContainer;
            byte[] message = iv.GetSignData(ProtocolSettings.Default.Network);
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            ECPoint pubkey = keyPair.PublicKey;
            byte[] signature = Crypto.Sign(message, privateKey, pubkey.EncodePoint(false).Skip(1).ToArray());
            engine.CheckSig(pubkey.EncodePoint(false), signature).Should().BeTrue();
            Action action = () => engine.CheckSig(new byte[70], signature);
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestCrypto_CheckMultiSig()
        {
            var engine = GetEngine(true);
            IVerifiable iv = engine.ScriptContainer;
            byte[] message = iv.GetSignData(ProtocolSettings.Default.Network);

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
            engine.CheckMultisig(pubkeys, signatures).Should().BeTrue();

            pubkeys = new byte[0][];
            Assert.ThrowsException<ArgumentException>(() => engine.CheckMultisig(pubkeys, signatures));

            pubkeys = new[]
            {
                pubkey1.EncodePoint(false),
                pubkey2.EncodePoint(false)
            };
            signatures = new byte[0][];
            Assert.ThrowsException<ArgumentException>(() => engine.CheckMultisig(pubkeys, signatures));

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
            engine.CheckMultisig(pubkeys, signatures).Should().BeFalse();

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
            Assert.ThrowsException<FormatException>(() => engine.CheckMultisig(pubkeys, signatures));
        }

        [TestMethod]
        public void TestContract_Create()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var nef = new NefFile()
            {
                Script = Enumerable.Repeat((byte)OpCode.RET, byte.MaxValue).ToArray(),
                Source = string.Empty,
                Compiler = "",
                Tokens = System.Array.Empty<MethodToken>()
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            var nefFile = nef.ToArray();
            var manifest = TestUtils.CreateDefaultManifest();
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(null, nefFile, manifest.ToJson().ToByteArray(false)));
            Assert.ThrowsException<ArgumentException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, new byte[ContractManifest.MaxLength + 1]));
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(true), 10000000));

            var script_exceedMaxLength = new NefFile()
            {
                Script = new byte[ExecutionEngineLimits.Default.MaxItemSize - 50],
                Source = string.Empty,
                Compiler = "",
                Tokens = Array.Empty<MethodToken>()
            };
            script_exceedMaxLength.CheckSum = NefFile.ComputeChecksum(script_exceedMaxLength);

            Assert.ThrowsException<FormatException>(() => script_exceedMaxLength.ToArray().AsSerializable<NefFile>());
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, script_exceedMaxLength.ToArray(), manifest.ToJson().ToByteArray(true)));

            var script_zeroLength = System.Array.Empty<byte>();
            Assert.ThrowsException<ArgumentException>(() => snapshot.DeployContract(UInt160.Zero, script_zeroLength, manifest.ToJson().ToByteArray(true)));

            var manifest_zeroLength = System.Array.Empty<byte>();
            Assert.ThrowsException<ArgumentException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest_zeroLength));

            manifest = TestUtils.CreateDefaultManifest();
            var ret = snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false));
            ret.Hash.ToString().Should().Be("0x7b37d4bd3d87f53825c3554bd1a617318235a685");
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false)));

            var state = TestUtils.GetContract();
            snapshot.AddContract(state.Hash, state);

            Assert.ThrowsException<InvalidOperationException>(() => snapshot.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false)));
        }

        [TestMethod]
        public void TestContract_Update()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var nef = new NefFile()
            {
                Script = new[] { (byte)OpCode.RET },
                Source = string.Empty,
                Compiler = "",
                Tokens = System.Array.Empty<MethodToken>()
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
                Value = new byte[] { 0x01 }
            };

            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };
            snapshot.AddContract(state.Hash, state);
            snapshot.Add(storageKey, storageItem);
            state.UpdateCounter.Should().Be(0);
            snapshot.UpdateContract(state.Hash, nef.ToArray(), manifest.ToJson().ToByteArray(false));
            var ret = NativeContract.ContractManagement.GetContract(snapshot, state.Hash);
            snapshot.Find(BitConverter.GetBytes(state.Id)).ToList().Count().Should().Be(1);
            ret.UpdateCounter.Should().Be(1);
            ret.Id.Should().Be(state.Id);
            ret.Manifest.ToJson().ToString().Should().Be(manifest.ToJson().ToString());
            ret.Script.Span.ToHexString().Should().Be(nef.Script.Span.ToHexString().ToString());
        }

        [TestMethod]
        public void TestContract_Update_Invalid()
        {
            var nefFile = new NefFile()
            {
                Script = new byte[] { 0x01 },
                Source = string.Empty,
                Compiler = "",
                Tokens = System.Array.Empty<MethodToken>()
            };
            nefFile.CheckSum = NefFile.ComputeChecksum(nefFile);

            var snapshot = TestBlockchain.GetTestSnapshot();

            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, null, new byte[] { 0x01 }));
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, nefFile.ToArray(), null));
            Assert.ThrowsException<ArgumentException>(() => snapshot.UpdateContract(null, null, null));

            nefFile = new NefFile()
            {
                Script = new byte[0],
                Source = string.Empty,
                Compiler = "",
                Tokens = System.Array.Empty<MethodToken>()
            };
            nefFile.CheckSum = NefFile.ComputeChecksum(nefFile);

            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, nefFile.ToArray(), new byte[] { 0x01 }));
            Assert.ThrowsException<InvalidOperationException>(() => snapshot.UpdateContract(null, nefFile.ToArray(), new byte[0]));
        }

        [TestMethod]
        public void TestStorage_Find()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var state = TestUtils.GetContract();

            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 }
            };
            var storageKey = new StorageKey
            {
                Id = state.Id,
                Key = new byte[] { 0x01 }
            };
            snapshot.AddContract(state.Hash, state);
            snapshot.Add(storageKey, storageItem);
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(new byte[] { 0x01 });

            var iterator = engine.Find(new StorageContext
            {
                Id = state.Id,
                IsReadOnly = false
            }, new byte[] { 0x01 }, FindOptions.ValuesOnly);
            iterator.Next();
            var ele = iterator.Value(null);
            ele.GetSpan().ToHexString().Should().Be(storageItem.Value.Span.ToHexString());
        }
    }
}
