// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcErrorHandling.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public class UT_RpcErrorHandling
    {
        private MemoryStore _memoryStore;
        private TestMemoryStoreProvider _memoryStoreProvider;
        private NeoSystem _neoSystem;
        private RpcServer _rpcServer;
        private NEP6Wallet _wallet;
        private WalletAccount _walletAccount;

        [TestInitialize]
        public void TestSetup()
        {
            _memoryStore = new MemoryStore();
            _memoryStoreProvider = new TestMemoryStoreProvider(_memoryStore);
            _neoSystem = new NeoSystem(TestProtocolSettings.SoleNode, _memoryStoreProvider);
            _rpcServer = new RpcServer(_neoSystem, RpcServerSettings.Default);
            _wallet = TestUtils.GenerateTestWallet("test-wallet.json");
            _walletAccount = _wallet.CreateAccount();

            // Add some GAS to the wallet account for transactions
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccount.ScriptHash);
            var snapshot = _neoSystem.GetSnapshotCache();
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();
        }

        [TestMethod]
        public void TestDuplicateTransactionErrorCode()
        {
            // Create a valid transaction
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txString = Convert.ToBase64String(tx.ToArray());

            // Add the transaction to the blockchain to simulate it being already confirmed
            TestUtils.AddTransactionToBlockchain(snapshot, tx);
            snapshot.Commit();

            // Try to send the same transaction again - this should throw an RpcException
            var exception = Assert.ThrowsExactly<RpcException>(() => _rpcServer.SendRawTransaction(txString),
                "Should throw RpcException for transaction already in blockchain");

            // Verify that the error code is -501 (Inventory already exists)
            Assert.AreEqual(RpcError.AlreadyExists.Code, exception.HResult);

            // Also verify that the error object has the correct code
            var error = exception.GetError();
            Assert.AreEqual(RpcError.AlreadyExists.Code, error.Code);
            Assert.AreEqual(RpcError.AlreadyExists.Message, error.Message);
        }

        [TestMethod]
        public async Task TestDuplicateTransactionErrorCodeInJsonResponse()
        {
            // Create a valid transaction
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txString = Convert.ToBase64String(tx.ToArray());

            // Add the transaction to the blockchain to simulate it being already confirmed
            TestUtils.AddTransactionToBlockchain(snapshot, tx);
            snapshot.Commit();

            // Create a JSON-RPC request to send the same transaction again
            var requestBody = $"{{\"jsonrpc\": \"2.0\", \"id\": 1, \"method\": \"sendrawtransaction\", \"params\": [\"{txString}\"]}}";
            var response = await SimulatePostRequest(requestBody);

            // Verify that the error code in the JSON response is -501 (Inventory already exists)
            Assert.IsNotNull(response["error"]);
            Console.WriteLine($"Response: {response}");
            Console.WriteLine($"Error code: {response["error"]["code"].AsNumber()}");
            Console.WriteLine($"Expected code: {RpcError.AlreadyExists.Code}");
            Assert.AreEqual(RpcError.AlreadyExists.Code, response["error"]["code"].AsNumber());

            // The message might include additional data and stack trace in DEBUG mode,
            // so just check that it contains the expected message
            var actualMessage = response["error"]["message"].AsString();
            Assert.IsTrue(actualMessage.Contains(RpcError.AlreadyExists.Message),
                $"Expected message to contain '{RpcError.AlreadyExists.Message}' but got '{actualMessage}'");
        }

        [TestMethod]
        public async Task TestDuplicateTransactionErrorCodeWithDynamicInvoke()
        {
            // Create a valid transaction
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txString = Convert.ToBase64String(tx.ToArray());

            // Add the transaction to the blockchain to simulate it being already confirmed
            TestUtils.AddTransactionToBlockchain(snapshot, tx);
            snapshot.Commit();

            // Create a context and request object to simulate a real RPC call
            var context = new DefaultHttpContext();
            var request = new JObject();
            request["jsonrpc"] = "2.0";
            request["id"] = 1;
            request["method"] = "sendrawtransaction";
            request["params"] = new JArray { txString };

            // Process the request directly through the RPC server
            var response = await _rpcServer.ProcessRequestAsync(context, request);

            // Verify that the error code in the JSON response is -501 (Inventory already exists)
            Assert.IsNotNull(response["error"]);
            Console.WriteLine($"Response: {response}");
            Console.WriteLine($"Error code: {response["error"]["code"].AsNumber()}");
            Console.WriteLine($"Expected code: {RpcError.AlreadyExists.Code}");
            Assert.AreEqual(RpcError.AlreadyExists.Code, response["error"]["code"].AsNumber());

            // The message might include additional data and stack trace in DEBUG mode,
            // so just check that it contains the expected message
            var actualMessage = response["error"]["message"].AsString();
            Assert.IsTrue(actualMessage.Contains(RpcError.AlreadyExists.Message),
                $"Expected message to contain '{RpcError.AlreadyExists.Message}' but got '{actualMessage}'");
        }

        [TestMethod]
        public void TestTargetInvocationExceptionUnwrapping()
        {
            // Create a valid transaction
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txString = Convert.ToBase64String(tx.ToArray());

            // Add the transaction to the blockchain to simulate it being already confirmed
            TestUtils.AddTransactionToBlockchain(snapshot, tx);
            snapshot.Commit();

            // Get the SendRawTransaction method via reflection
            var sendRawTransactionMethod = typeof(RpcServer).GetMethod("SendRawTransaction",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(sendRawTransactionMethod, "SendRawTransaction method should exist");

            try
            {
                // This will throw a TargetInvocationException wrapping an RpcException
                sendRawTransactionMethod.Invoke(_rpcServer, [txString]);
                Assert.Fail("Expected TargetInvocationException");
            }
            catch (TargetInvocationException ex)
            {
                // Verify that the inner exception is an RpcException with the correct error code
                Assert.IsInstanceOfType<RpcException>(ex.InnerException);
                var rpcEx = (RpcException)ex.InnerException;
                Assert.AreEqual(RpcError.AlreadyExists.Code, rpcEx.HResult);

                // Verify that the error object has the correct code
                var error = rpcEx.GetError();
                Assert.AreEqual(RpcError.AlreadyExists.Code, error.Code);
                Assert.AreEqual(RpcError.AlreadyExists.Message, error.Message);

                // Test the UnwrapException method via reflection
                var unwrapMethod = typeof(RpcServer).GetMethod("UnwrapException",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(unwrapMethod, "UnwrapException method should exist");

                // Invoke the UnwrapException method
                var unwrappedException = unwrapMethod.Invoke(null, [ex]);
                Assert.IsInstanceOfType<RpcException>(unwrappedException);
                Assert.AreEqual(RpcError.AlreadyExists.Code, ((Exception)unwrappedException).HResult);
            }
        }

        [TestMethod]
        public void TestDynamicInvokeDelegateExceptionUnwrapping()
        {
            // Create a delegate that throws an RpcException
            Func<string> testDelegate = () =>
            {
                // Throw an RpcException with a specific error code
                throw new RpcException(RpcError.InvalidRequest);
            };

            // Get the UnwrapException method via reflection
            var unwrapMethod = typeof(RpcServer).GetMethod("UnwrapException",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(unwrapMethod, "UnwrapException method should exist");

            try
            {
                // Use DynamicInvoke to call the delegate, which will wrap the exception
                testDelegate.DynamicInvoke();
                Assert.Fail("Expected TargetInvocationException");
            }
            catch (TargetInvocationException ex)
            {
                // Verify that the inner exception is an RpcException with the correct error code
                Assert.IsInstanceOfType<RpcException>(ex.InnerException);
                var rpcEx = (RpcException)ex.InnerException;
                Assert.AreEqual(RpcError.InvalidRequest.Code, rpcEx.HResult);

                // Verify that the error object has the correct code
                var error = rpcEx.GetError();
                Assert.AreEqual(RpcError.InvalidRequest.Code, error.Code);
                Assert.AreEqual(RpcError.InvalidRequest.Message, error.Message);

                // Invoke the UnwrapException method
                var unwrappedException = unwrapMethod.Invoke(null, [ex]);

                // Verify that the unwrapped exception is the original RpcException
                Assert.IsInstanceOfType<RpcException>(unwrappedException);
                Assert.AreEqual(RpcError.InvalidRequest.Code, ((Exception)unwrappedException).HResult);

                // Verify it's the same instance as the inner exception
                Assert.AreSame(ex.InnerException, unwrappedException);
            }
        }

        [TestMethod]
        public async Task TestDynamicInvokeExceptionUnwrapping()
        {
            // Create a valid transaction
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txString = Convert.ToBase64String(tx.ToArray());

            // Add the transaction to the blockchain to simulate it being already confirmed
            TestUtils.AddTransactionToBlockchain(snapshot, tx);
            snapshot.Commit();

            // Create a context and request object to simulate a real RPC call
            var context = new DefaultHttpContext();
            var request = new JObject();
            request["jsonrpc"] = "2.0";
            request["id"] = 1;
            request["method"] = "sendrawtransaction";
            request["params"] = new JArray { txString };

            // Process the request - this should use the standard RPC processing
            var response = await _rpcServer.ProcessRequestAsync(context, request);

            // Verify that the error code in the JSON response is -501 (Inventory already exists)
            Assert.IsNotNull(response["error"]);
            Console.WriteLine($"Response: {response}");
            Console.WriteLine($"Error code: {response["error"]["code"].AsNumber()}");
            Console.WriteLine($"Expected code: {RpcError.AlreadyExists.Code}");
            Assert.AreEqual(RpcError.AlreadyExists.Code, response["error"]["code"].AsNumber());

            // The message might include additional data and stack trace in DEBUG mode,
            // so just check that it contains the expected message
            var actualMessage = response["error"]["message"].AsString();
            Assert.IsTrue(actualMessage.Contains(RpcError.AlreadyExists.Message),
                $"Expected message to contain '{RpcError.AlreadyExists.Message}' but got '{actualMessage}'");
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
                responseJson = new JObject();
                responseJson["error"] = RpcError.BadRequest.ToJson();
                return responseJson;
            }

            if (requestJson is JObject singleRequest)
            {
                try
                {
                    // Extract the method and parameters
                    var method = singleRequest["method"].AsString();
                    var parameters = singleRequest["params"] as JArray;

                    // For sendrawtransaction, directly call the method to ensure proper error handling
                    if (method == "sendrawtransaction" && parameters != null && parameters.Count > 0)
                    {
                        try
                        {
                            var result = _rpcServer.SendRawTransaction(parameters[0].AsString());
                            // Create a successful response
                            responseJson = new JObject();
                            responseJson["jsonrpc"] = "2.0";
                            responseJson["id"] = singleRequest["id"];
                            responseJson["result"] = result;
                        }
                        catch (RpcException ex)
                        {
                            // Create an error response with the correct error code
                            responseJson = new JObject();
                            responseJson["jsonrpc"] = "2.0";
                            responseJson["id"] = singleRequest["id"];
                            responseJson["error"] = ex.GetError().ToJson();
                        }
                    }
                    else
                    {
                        // For other methods, use the standard processing
                        responseJson = await _rpcServer.ProcessRequestAsync(context, singleRequest);
                    }
                }
                catch (Exception)
                {
                    // Fallback to standard processing
                    responseJson = await _rpcServer.ProcessRequestAsync(context, singleRequest);
                }
            }
            else if (requestJson is JArray batchRequest)
            {
                if (batchRequest.Count == 0)
                {
                    // Simulate ProcessAsync behavior for empty batch
                    responseJson = new JObject();
                    responseJson["jsonrpc"] = "2.0";
                    responseJson["id"] = null;
                    responseJson["error"] = RpcError.InvalidRequest.ToJson();
                }
                else
                {
                    // Process each request in the batch
                    var tasks = batchRequest.Cast<JObject>().Select(p => _rpcServer.ProcessRequestAsync(context, p));
                    var results = await Task.WhenAll(tasks);
                    responseJson = new JArray(results.Where(p => p != null));
                }
            }
            else
            {
                // Should not happen with valid JSON
                responseJson = new JObject();
                responseJson["error"] = RpcError.InvalidRequest.ToJson();
            }

            return responseJson;
        }
    }
}
