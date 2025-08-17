// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StatePlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Cryptography.MPTTrie;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence.Providers;
using Neo.Plugins.RpcServer;
using Neo.Plugins.StateService.Network;
using Neo.Plugins.StateService.Storage;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.VM;
using System.Reflection;

namespace Neo.Plugins.StateService.Tests
{
    [TestClass]
    public class UT_StatePlugin
    {
        private const uint TestNetwork = 5195086u;
        private const string RootHashHex = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        private const string ScriptHashHex = "0x1234567890abcdef1234567890abcdef12345678";

        private static readonly ProtocolSettings s_protocol = TestProtocolSettings.Default with { Network = TestNetwork };

        private StatePlugin? _statePlugin;
        private TestBlockchain.TestNeoSystem? _system;
        private MemoryStore? _memoryStore;

        [TestInitialize]
        public void Setup()
        {
            _memoryStore = new MemoryStore();
            _system = new TestBlockchain.TestNeoSystem(s_protocol);
            _statePlugin = new StatePlugin();

            // Use reflection to call the protected OnSystemLoaded method
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var onSystemLoaded = typeof(StatePlugin).GetMethod("OnSystemLoaded", bindingFlags);
            Assert.IsNotNull(onSystemLoaded, "OnSystemLoaded method not found via reflection.");

            onSystemLoaded.Invoke(_statePlugin, [_system]);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PluginConfiguration:FullState"] = "true",
                    ["PluginConfiguration:Network"] = TestNetwork.ToString(),
                })
                .Build()
                .GetSection("PluginConfiguration");
            StateServiceSettings.Load(config);
            Assert.IsTrue(StateServiceSettings.Default.FullState);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _statePlugin?.Dispose();
            _memoryStore?.Dispose();
        }

        [TestMethod]
        public void TestGetStateHeight_Basic()
        {
            var result = _statePlugin!.GetStateHeight();

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));

            Assert.IsNull(result!["localrootindex"]);
            Assert.IsNull(result!["validatedrootindex"]);
        }

        [TestMethod]
        public void TestGetStateRoot_WithInvalidIndex_ShouldThrowRpcException()
        {
            var exception = Assert.ThrowsExactly<RpcException>(() => _statePlugin!.GetStateRoot(999));
            Assert.AreEqual(RpcError.UnknownStateRoot.Code, exception.HResult);
        }

        [TestMethod]
        public void TestGetProof_WithInvalidKey_ShouldThrowRpcException()
        {
            var rootHash = UInt256.Parse(RootHashHex);
            var scriptHash = UInt160.Parse(ScriptHashHex);
            var invalidKey = "invalid_base64_string";

            var exception = Assert.ThrowsExactly<RpcException>(() => _statePlugin!.GetProof(rootHash, scriptHash, invalidKey));
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }

        [TestMethod]
        public void TestVerifyProof_WithInvalidProof_ShouldThrowRpcException()
        {
            var rootHash = UInt256.Parse(RootHashHex);
            var invalidProof = "invalid_proof_string";

            var exception = Assert.ThrowsExactly<RpcException>(() => _statePlugin!.VerifyProof(rootHash, invalidProof));
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }


        [TestMethod]
        public void TestGetStateRoot_WithMockData_ShouldReturnStateRoot()
        {
            SetupMockStateRoot(1, UInt256.Parse(RootHashHex));
            var result = _statePlugin!.GetStateRoot(1);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));

            var json = (JObject)result;
            Assert.AreEqual(0x00, json["version"]?.AsNumber());
            Assert.AreEqual(1u, json["index"]?.AsNumber());
            Assert.IsNotNull(json["roothash"]);
            Assert.IsNotNull(json["witnesses"]);
        }

        [TestMethod]
        public void TestGetProof_WithMockData_ShouldReturnProof()
        {
            Assert.IsTrue(StateServiceSettings.Default.FullState);

            var scriptHash = UInt160.Parse(ScriptHashHex);
            var rootHash = SetupMockContractAndStorage(scriptHash);
            SetupMockStateRoot(1, rootHash);

            var result = _statePlugin!.GetProof(rootHash, scriptHash, Convert.ToBase64String([0x01, 0x02]));

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JString));

            var proof = ((JString)result).Value; // long string
            Assert.IsFalse(string.IsNullOrEmpty(proof));
        }

        [TestMethod]
        public void TestGetState_WithMockData_ShouldReturnValue()
        {
            var scriptHash = UInt160.Parse(ScriptHashHex);

            var rootHash = SetupMockContractAndStorage(scriptHash);
            SetupMockStateRoot(1, rootHash);

            var result = _statePlugin!.GetState(rootHash, scriptHash, [0x01, 0x02]);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JString));
            Assert.AreEqual("aabb", Convert.FromBase64String(result.AsString() ?? "").ToHexString());
        }

        [TestMethod]
        public void TestFindStates_WithMockData_ShouldReturnResults()
        {
            var scriptHash = UInt160.Parse(ScriptHashHex);
            var rootHash = SetupMockContractAndStorage(scriptHash);
            SetupMockStateRoot(1, rootHash);

            var result = _statePlugin!.FindStates(rootHash, scriptHash, []);
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(JObject));

            var jsonResult = (JObject)result;
            Assert.IsNotNull(jsonResult["results"]);
            Assert.IsInstanceOfType(jsonResult["results"], typeof(JArray));

            var results = (JArray)jsonResult["results"]!;
            Assert.AreEqual(2, results.Count);

            Assert.AreEqual("0102", Convert.FromBase64String(results[0]?["key"]?.AsString() ?? "").ToHexString());
            Assert.AreEqual("0304", Convert.FromBase64String(results[1]?["key"]?.AsString() ?? "").ToHexString());
            Assert.AreEqual("aabb", Convert.FromBase64String(results[0]?["value"]?.AsString() ?? "").ToHexString());
            Assert.AreEqual("ccdd", Convert.FromBase64String(results[1]?["value"]?.AsString() ?? "").ToHexString());
            Assert.IsFalse(jsonResult["truncated"]?.AsBoolean());
        }

        private void SetupMockStateRoot(uint index, UInt256 rootHash)
        {
            var stateRoot = new StateRoot { Index = index, RootHash = rootHash, Witness = Witness.Empty };
            using var store = StateStore.Singleton.GetSnapshot();
            store.AddLocalStateRoot(stateRoot);
            store.Commit();
        }

        private UInt256 SetupMockContractAndStorage(UInt160 scriptHash)
        {
            var nef = new NefFile { Compiler = "mock", Source = "mock", Tokens = [], Script = new byte[] { 0x01 } };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            var contractState = new ContractState
            {
                Id = 1,
                Hash = scriptHash,
                Nef = nef,
                Manifest = new ContractManifest()
                {
                    Name = "TestContract",
                    Groups = [],
                    SupportedStandards = [],
                    Abi = new ContractAbi() { Methods = [], Events = [] },
                    Permissions = [],
                    Trusts = WildcardContainer<ContractPermissionDescriptor>.CreateWildcard(),
                }
            };

            var contractKey = new StorageKey
            {
                Id = NativeContract.ContractManagement.Id,
                Key = new byte[] { 8 }.Concat(scriptHash.ToArray()).ToArray(),
            };

            var contractValue = BinarySerializer.Serialize(contractState.ToStackItem(null), ExecutionEngineLimits.Default);

            using var storeSnapshot = StateStore.Singleton.GetStoreSnapshot();
            var trie = new Trie(storeSnapshot, null);
            trie.Put(contractKey.ToArray(), contractValue);

            var key1 = new StorageKey { Id = 1, Key = new byte[] { 0x01, 0x02 } };
            var value1 = new StorageItem { Value = new byte[] { 0xaa, 0xbb } };
            trie.Put(key1.ToArray(), value1.ToArray());

            var key2 = new StorageKey { Id = 1, Key = new byte[] { 0x03, 0x04 } };
            var value2 = new StorageItem { Value = new byte[] { 0xcc, 0xdd } };
            trie.Put(key2.ToArray(), value2.ToArray());

            trie.Commit();
            storeSnapshot.Commit();

            return trie.Root.Hash;
        }
    }
}

