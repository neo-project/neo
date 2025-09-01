// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public partial class UT_RpcServer
    {
        private NeoSystem _neoSystem;
        private RpcServersSettings _rpcServerSettings;
        private RpcServer _rpcServer;
        private TestMemoryStoreProvider _memoryStoreProvider;
        private MemoryStore _memoryStore;
        private readonly NEP6Wallet _wallet = TestUtils.GenerateTestWallet("123");
        private WalletAccount _walletAccount;

        [TestInitialize]
        public void TestSetup()
        {
            _memoryStore = new MemoryStore();
            _memoryStoreProvider = new TestMemoryStoreProvider(_memoryStore);
            _neoSystem = new NeoSystem(TestProtocolSettings.SoleNode, _memoryStoreProvider);
            _rpcServerSettings = RpcServersSettings.Default with
            {
                SessionEnabled = true,
                SessionExpirationTime = TimeSpan.FromSeconds(0.3),
                MaxGasInvoke = 1500_0000_0000,
                Network = TestProtocolSettings.SoleNode.Network,
            };
            _rpcServer = new RpcServer(_neoSystem, _rpcServerSettings);
            _walletAccount = _wallet.Import("KxuRSsHgJMb3AMSN6B9P3JHNGMFtxmuimqgR9MmXPcv3CLLfusTd");
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccount.ScriptHash);
            var snapshot = _neoSystem.GetSnapshotCache();
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Please build and test in debug mode
            _neoSystem.MemPool.Clear();
            _memoryStore.Reset();
            var snapshot = _neoSystem.GetSnapshotCache();
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccount.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();
        }

        [TestMethod]
        public void TestCheckAuth_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:testpass"));
            // Act
            var result = _rpcServer.CheckAuth(context);
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCheckAuth()
        {
            var memoryStoreProvider = new TestMemoryStoreProvider(new MemoryStore());
            var neoSystem = new NeoSystem(TestProtocolSettings.SoleNode, memoryStoreProvider);
            var rpcServerSettings = RpcServersSettings.Default with
            {
                SessionEnabled = true,
                SessionExpirationTime = TimeSpan.FromSeconds(0.3),
                MaxGasInvoke = 1500_0000_0000,
                Network = TestProtocolSettings.SoleNode.Network,
                RpcUser = "testuser",
                RpcPass = "testpass",
            };
            var rpcServer = new RpcServer(neoSystem, rpcServerSettings);

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:testpass"));
            var result = rpcServer.CheckAuth(context);
            Assert.IsTrue(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:wrongpass"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("wronguser:testpass"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(":testpass"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(""));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);
        }

        // Helper to simulate processing a raw POST request
        private async Task<JToken> SimulatePostRequest(string requestBody)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            context.Request.ContentType = "application/json";

            JToken requestJson = null;
            JToken responseJson = null;
            try
            {
                requestJson = JToken.Parse(requestBody);
            }
            catch (FormatException)
            {
                // Simulate ProcessAsync behavior for malformed JSON
                return new JObject() { ["error"] = RpcError.BadRequest.ToJson() };
            }

            if (requestJson is JObject singleRequest)
            {
                responseJson = await _rpcServer.ProcessRequestAsync(context, singleRequest);
            }
            else if (requestJson is JArray batchRequest)
            {
                if (batchRequest.Count == 0)
                {
                    // Simulate ProcessAsync behavior for empty batch
                    responseJson = new JObject()
                    {
                        ["jsonrpc"] = "2.0",
                        ["id"] = null,
                        ["error"] = RpcError.InvalidRequest.ToJson(),
                    };
                }
                else
                {
                    // Ensure Cast<JObject> refers to Neo.Json.JObject
                    var tasks = batchRequest.Cast<JObject>().Select(p => _rpcServer.ProcessRequestAsync(context, p));
                    var results = await Task.WhenAll(tasks);
                    // Ensure new JArray is Neo.Json.JArray
                    responseJson = new JArray(results.Where(p => p != null));
                }
            }
            else
            {
                // Should not happen with valid JSON
                // Revert to standard assignment
                responseJson = new JObject() { ["error"] = RpcError.InvalidRequest.ToJson() };
            }

            return responseJson;
        }

        [TestMethod]
        public async Task TestProcessRequest_MalformedJsonPostBody()
        {
            var malformedJson = """{"jsonrpc": "2.0", "method": "getblockcount", "params": [], "id": 1"""; // Missing closing brace
            var response = await SimulatePostRequest(malformedJson);

            Assert.IsNotNull(response["error"]);
            Assert.AreEqual(RpcError.BadRequest.Code, response["error"]["code"].AsNumber());
        }

        [TestMethod]
        public async Task TestProcessRequest_EmptyBatch()
        {
            var emptyBatchJson = "[]";
            var response = await SimulatePostRequest(emptyBatchJson);

            Assert.IsNotNull(response["error"]);
            Assert.AreEqual(RpcError.InvalidRequest.Code, response["error"]["code"].AsNumber());
        }

        [TestMethod]
        public async Task TestProcessRequest_MixedBatch()
        {
            var mixedBatchJson = """
            [
                {"jsonrpc": "2.0", "method": "getblockcount", "params": [], "id": 1},
                {"jsonrpc": "2.0", "method": "nonexistentmethod", "params": [], "id": 2},
                {"jsonrpc": "2.0", "method": "getblock", "params": ["invalid_index"], "id": 3},
                {"jsonrpc": "2.0", "method": "getversion", "id": 4}
            ]
            """;

            var response = await SimulatePostRequest(mixedBatchJson);
            Assert.IsInstanceOfType(response, typeof(JArray));
            var batchResults = (JArray)response;

            Assert.HasCount(4, batchResults);

            // Check response 1 (valid getblockcount)
            Assert.IsNull(batchResults[0]["error"]);
            Assert.IsNotNull(batchResults[0]["result"]);
            Assert.AreEqual(1, batchResults[0]["id"].AsNumber());

            // Check response 2 (invalid method)
            Assert.IsNotNull(batchResults[1]["error"]);
            Assert.AreEqual(RpcError.MethodNotFound.Code, batchResults[1]["error"]["code"].AsNumber());
            Assert.AreEqual(2, batchResults[1]["id"].AsNumber());

            // Check response 3 (invalid params for getblock)
            Assert.IsNotNull(batchResults[2]["error"]);
            Assert.AreEqual(RpcError.InvalidParams.Code, batchResults[2]["error"]["code"].AsNumber());
            Assert.AreEqual(3, batchResults[2]["id"].AsNumber());

            // Check response 4 (valid getversion)
            Assert.IsNull(batchResults[3]["error"]);
            Assert.IsNotNull(batchResults[3]["result"]);
            Assert.AreEqual(4, batchResults[3]["id"].AsNumber());
        }

        private class MockRpcMethods
        {
#nullable enable
            [RpcMethod]
            public JToken GetMockMethod(string info) => $"mock {info}";

            public JToken NullContextMethod(string? info) => $"mock {info}";
#nullable restore

#nullable disable
            public JToken NullableMethod(string info) => $"mock {info}";

            public JToken OptionalMethod(string info = "default") => $"mock {info}";
#nullable restore
        }

        [TestMethod]
        public async Task TestRegisterMethods()
        {
            _rpcServer.RegisterMethods(new MockRpcMethods());

            // Request ProcessAsync with a valid request
            var context = new DefaultHttpContext();
            var body = """
            {"jsonrpc": "2.0", "method": "getmockmethod", "params": ["test"], "id": 1 }
            """;
            context.Request.Method = "POST";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            context.Request.ContentType = "application/json";

            // Set up a writable response body
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _rpcServer.ProcessAsync(context);
            Assert.IsNotNull(context.Response.Body);

            // Reset the stream position to read from the beginning
            responseBody.Position = 0;
            var output = new StreamReader(responseBody).ReadToEnd();

            // Parse the JSON response and check the result
            var responseJson = JToken.Parse(output);
            Assert.IsNotNull(responseJson["result"]);
            Assert.AreEqual("mock test", responseJson["result"].AsString());
            Assert.AreEqual(200, context.Response.StatusCode);
        }

        [TestMethod]
        public void TestNullableParameter()
        {
            var method = typeof(MockRpcMethods).GetMethod("GetMockMethod");
            var parameter = RpcServer.AsRpcParameter(method.GetParameters()[0]);
            Assert.IsTrue(parameter.Required);
            Assert.AreEqual(typeof(string), parameter.Type);
            Assert.AreEqual("info", parameter.Name);

            method = typeof(MockRpcMethods).GetMethod("NullableMethod");
            parameter = RpcServer.AsRpcParameter(method.GetParameters()[0]);
            Assert.IsFalse(parameter.Required);
            Assert.AreEqual(typeof(string), parameter.Type);
            Assert.AreEqual("info", parameter.Name);

            method = typeof(MockRpcMethods).GetMethod("NullContextMethod");
            parameter = RpcServer.AsRpcParameter(method.GetParameters()[0]);
            Assert.IsFalse(parameter.Required);
            Assert.AreEqual(typeof(string), parameter.Type);
            Assert.AreEqual("info", parameter.Name);

            method = typeof(MockRpcMethods).GetMethod("OptionalMethod");
            parameter = RpcServer.AsRpcParameter(method.GetParameters()[0]);
            Assert.IsFalse(parameter.Required);
            Assert.AreEqual(typeof(string), parameter.Type);
            Assert.AreEqual("info", parameter.Name);
            Assert.AreEqual("default", parameter.DefaultValue);
        }

        [TestMethod]
        public void TestRpcServerSettings_Load()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("RpcServer.json")
                .Build()
                .GetSection("PluginConfiguration")
                .GetSection("Servers")
                .GetChildren()
                .First();

            var settings = RpcServersSettings.Load(config);
            Assert.AreEqual(860833102u, settings.Network);
            Assert.AreEqual(10332, settings.Port);
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), settings.BindAddress);
            Assert.AreEqual(string.Empty, settings.SslCert);
            Assert.AreEqual(string.Empty, settings.SslCertPassword);
            Assert.AreEqual(0, settings.TrustedAuthorities.Length);
            Assert.AreEqual(string.Empty, settings.RpcUser);
            Assert.AreEqual(string.Empty, settings.RpcPass);
            Assert.AreEqual(true, settings.EnableCors);
            Assert.AreEqual(20_00000000, settings.MaxGasInvoke);
            Assert.AreEqual(TimeSpan.FromSeconds(60), settings.SessionExpirationTime);
            Assert.AreEqual(false, settings.SessionEnabled);
            Assert.AreEqual(true, settings.EnableCors);
            Assert.AreEqual(0, settings.AllowOrigins.Length);
            Assert.AreEqual(60, settings.KeepAliveTimeout);
            Assert.AreEqual(15u, settings.RequestHeadersTimeout);
            Assert.AreEqual(1000_0000, settings.MaxFee); // 0.1 * 10^8
            Assert.AreEqual(100, settings.MaxIteratorResultItems);
            Assert.AreEqual(65535, settings.MaxStackSize);
            Assert.AreEqual(1, settings.DisabledMethods.Length);
            Assert.AreEqual("openwallet", settings.DisabledMethods[0]);
            Assert.AreEqual(40, settings.MaxConcurrentConnections);
            Assert.AreEqual(5 * 1024 * 1024, settings.MaxRequestBodySize);
            Assert.AreEqual(50, settings.FindStoragePageSize);
        }
    }
}
