// Copyright (C) 2015-2024 The Neo Project.
//
// UT_InteropService.NEO.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P;
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
            var iv = engine.ScriptContainer;
            var message = iv.GetSignData(TestProtocolSettings.Default.Network);
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            var keyPair = new KeyPair(privateKey);
            var pubkey = keyPair.PublicKey;
            var signature = Crypto.Sign(message, privateKey);
            engine.CheckSig(pubkey.EncodePoint(false), signature).Should().BeTrue();
            Action action = () => engine.CheckSig(new byte[70], signature);
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestCrypto_CheckMultiSig()
        {
            var engine = GetEngine(true);
            var iv = engine.ScriptContainer;
            var message = iv.GetSignData(TestProtocolSettings.Default.Network);

            byte[] privkey1 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            var key1 = new KeyPair(privkey1);
            var pubkey1 = key1.PublicKey;
            var signature1 = Crypto.Sign(message, privkey1);

            byte[] privkey2 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02};
            var key2 = new KeyPair(privkey2);
            var pubkey2 = key2.PublicKey;
            var signature2 = Crypto.Sign(message, privkey2);

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
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
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
            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.DeployContract(null, nefFile, manifest.ToJson().ToByteArray(false)));
            Assert.ThrowsException<ArgumentException>(() => snapshotCache.DeployContract(UInt160.Zero, nefFile, new byte[ContractManifest.MaxLength + 1]));
            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(true), 10000000));

            var script_exceedMaxLength = new NefFile()
            {
                Script = new byte[ExecutionEngineLimits.Default.MaxItemSize - 50],
                Source = string.Empty,
                Compiler = "",
                Tokens = Array.Empty<MethodToken>()
            };
            script_exceedMaxLength.CheckSum = NefFile.ComputeChecksum(script_exceedMaxLength);

            Assert.ThrowsException<FormatException>(() => script_exceedMaxLength.ToArray().AsSerializable<NefFile>());
            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.DeployContract(UInt160.Zero, script_exceedMaxLength.ToArray(), manifest.ToJson().ToByteArray(true)));

            var script_zeroLength = System.Array.Empty<byte>();
            Assert.ThrowsException<ArgumentException>(() => snapshotCache.DeployContract(UInt160.Zero, script_zeroLength, manifest.ToJson().ToByteArray(true)));

            var manifest_zeroLength = System.Array.Empty<byte>();
            Assert.ThrowsException<ArgumentException>(() => snapshotCache.DeployContract(UInt160.Zero, nefFile, manifest_zeroLength));

            manifest = TestUtils.CreateDefaultManifest();
            var ret = snapshotCache.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false));
            ret.Hash.ToString().Should().Be("0x7b37d4bd3d87f53825c3554bd1a617318235a685");
            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false)));

            var state = TestUtils.GetContract();
            snapshotCache.AddContract(state.Hash, state);

            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.DeployContract(UInt160.Zero, nefFile, manifest.ToJson().ToByteArray(false)));
        }

        [TestMethod]
        public void TestContract_Update()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var nef = new NefFile()
            {
                Script = new[] { (byte)OpCode.RET },
                Source = string.Empty,
                Compiler = "",
                Tokens = Array.Empty<MethodToken>()
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.UpdateContract(null, nef.ToArray(), new byte[0]));

            var manifest = TestUtils.CreateDefaultManifest();
            byte[] privkey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            var key = new KeyPair(privkey);
            var pubkey = key.PublicKey;
            var state = TestUtils.GetContract();
            var signature = Crypto.Sign(state.Hash.ToArray(), privkey);
            manifest.Groups = new ContractGroup[]
            {
                new()
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
            snapshotCache.AddContract(state.Hash, state);
            snapshotCache.Add(storageKey, storageItem);
            state.UpdateCounter.Should().Be(0);
            snapshotCache.UpdateContract(state.Hash, nef.ToArray(), manifest.ToJson().ToByteArray(false));
            var ret = NativeContract.ContractManagement.GetContract(snapshotCache, state.Hash);
            snapshotCache.Find(BitConverter.GetBytes(state.Id)).ToList().Count().Should().Be(1);
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

            var snapshotCache = TestBlockchain.GetTestSnapshotCache();

            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.UpdateContract(null, null, new byte[] { 0x01 }));
            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.UpdateContract(null, nefFile.ToArray(), null));
            Assert.ThrowsException<ArgumentException>(() => snapshotCache.UpdateContract(null, null, null));

            nefFile = new NefFile()
            {
                Script = new byte[0],
                Source = string.Empty,
                Compiler = "",
                Tokens = System.Array.Empty<MethodToken>()
            };
            nefFile.CheckSum = NefFile.ComputeChecksum(nefFile);

            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.UpdateContract(null, nefFile.ToArray(), new byte[] { 0x01 }));
            Assert.ThrowsException<InvalidOperationException>(() => snapshotCache.UpdateContract(null, nefFile.ToArray(), new byte[0]));
        }

        [TestMethod]
        public void TestStorage_Find()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
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
            snapshotCache.AddContract(state.Hash, state);
            snapshotCache.Add(storageKey, storageItem);
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache);
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
