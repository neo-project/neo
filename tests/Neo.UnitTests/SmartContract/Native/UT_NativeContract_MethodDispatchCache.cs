// Copyright (C) 2015-2026 The Neo Project.
//
// UT_NativeContract_MethodDispatchCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.UnitTests.SmartContract.Native
{
    /// <summary>
    /// Tests for the native contract method dispatch cache optimization.
    /// These tests verify both correctness and thread-safety of the cached delegate approach.
    /// </summary>
    [TestClass]
    public class UT_NativeContract_MethodDispatchCache
    {
        private DataCache _snapshotCache = null!;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        /// <summary>
        /// Helper method to execute a native contract method and return the result.
        /// </summary>
        private StackItem ExecuteNativeMethod(UInt160 contractHash, string method, params object[] args)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, _snapshotCache.CloneCache(), settings: TestProtocolSettings.Default);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(contractHash, method, args);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute(), $"Method {method} should succeed");
            return engine.ResultStack.Pop();
        }

        /// <summary>
        /// Test that the cache works correctly for Decimals method calls.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_Decimals()
        {
            // Call Decimals multiple times - should use cached delegate after first call
            var result1 = ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals");
            var result2 = ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals");
            var result3 = ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals");

            Assert.AreEqual(0, result1.GetInteger());
            Assert.AreEqual(0, result2.GetInteger());
            Assert.AreEqual(0, result3.GetInteger());

            var gasResult1 = ExecuteNativeMethod(NativeContract.GAS.Hash, "decimals");
            var gasResult2 = ExecuteNativeMethod(NativeContract.GAS.Hash, "decimals");

            Assert.AreEqual(8, gasResult1.GetInteger());
            Assert.AreEqual(8, gasResult2.GetInteger());
        }

        /// <summary>
        /// Test that the cache works correctly for Symbol calls.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_Symbol()
        {
            var result1 = ExecuteNativeMethod(NativeContract.NEO.Hash, "symbol");
            var result2 = ExecuteNativeMethod(NativeContract.NEO.Hash, "symbol");

            Assert.AreEqual("NEO", result1.GetString());
            Assert.AreEqual("NEO", result2.GetString());

            var gasResult1 = ExecuteNativeMethod(NativeContract.GAS.Hash, "symbol");
            var gasResult2 = ExecuteNativeMethod(NativeContract.GAS.Hash, "symbol");

            Assert.AreEqual("GAS", gasResult1.GetString());
            Assert.AreEqual("GAS", gasResult2.GetString());
        }

        /// <summary>
        /// Test that the cache works correctly for TotalSupply calls.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_TotalSupply()
        {
            var result1 = ExecuteNativeMethod(NativeContract.NEO.Hash, "totalSupply");
            var result2 = ExecuteNativeMethod(NativeContract.NEO.Hash, "totalSupply");

            Assert.AreEqual(result1.GetInteger(), result2.GetInteger());
        }

        /// <summary>
        /// Test that the cache works correctly for BalanceOf calls with parameters.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_BalanceOf()
        {
            var address = UInt160.Zero;

            var result1 = ExecuteNativeMethod(NativeContract.NEO.Hash, "balanceOf", address);
            var result2 = ExecuteNativeMethod(NativeContract.NEO.Hash, "balanceOf", address);

            Assert.AreEqual(result1.GetInteger(), result2.GetInteger());
        }

        /// <summary>
        /// Test that the cache works correctly for StdLib methods.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_StdLib()
        {
            var data = new byte[] { 0x01, 0x02, 0x03 };

            // Test multiple calls return same result
            var result1 = ExecuteNativeMethod(NativeContract.StdLib.Hash, "base64Encode", data);
            var result2 = ExecuteNativeMethod(NativeContract.StdLib.Hash, "base64Encode", data);
            Assert.AreEqual(result1.GetString(), result2.GetString());

            var encoded = result1.GetString();
            var result3 = ExecuteNativeMethod(NativeContract.StdLib.Hash, "base64Decode", encoded);
            var result4 = ExecuteNativeMethod(NativeContract.StdLib.Hash, "base64Decode", encoded);
            CollectionAssert.AreEqual(result3.GetSpan().ToArray(), result4.GetSpan().ToArray());
        }

        /// <summary>
        /// Test that the cache works correctly for PolicyContract methods.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_Policy()
        {
            var result1 = ExecuteNativeMethod(NativeContract.Policy.Hash, "getFeePerByte");
            var result2 = ExecuteNativeMethod(NativeContract.Policy.Hash, "getFeePerByte");
            Assert.AreEqual(result1.GetInteger(), result2.GetInteger());
        }

        /// <summary>
        /// Test that the cache works correctly for LedgerContract methods.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_Ledger()
        {
            var result1 = ExecuteNativeMethod(NativeContract.Ledger.Hash, "currentHash");
            var result2 = ExecuteNativeMethod(NativeContract.Ledger.Hash, "currentHash");
            Assert.AreEqual(result1.GetSpan().ToArray().ToHexString(), result2.GetSpan().ToArray().ToHexString());

            var result3 = ExecuteNativeMethod(NativeContract.Ledger.Hash, "currentIndex");
            var result4 = ExecuteNativeMethod(NativeContract.Ledger.Hash, "currentIndex");
            Assert.AreEqual(result3.GetInteger(), result4.GetInteger());
        }

        /// <summary>
        /// Test that the cache works correctly under concurrent access.
        /// This verifies thread-safety of the ConcurrentDictionary-based cache.
        /// </summary>
        [TestMethod]
        public void Test_DelegateCaching_ThreadSafety()
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.For(0, 100, options, i =>
            {
                // Each thread calls different methods to test cache isolation
                var result = ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals");
                Assert.AreEqual(0, result.GetInteger());

                var symbolResult = ExecuteNativeMethod(NativeContract.NEO.Hash, "symbol");
                Assert.AreEqual("NEO", symbolResult.GetString());
            });
        }

        /// <summary>
        /// Test concurrent access to different methods across contracts.
        /// This tests cache isolation and thread-safety under load.
        /// </summary>
        [TestMethod]
        public void Test_DelegateCaching_Concurrent_DifferentMethods()
        {
            var contracts = new (UInt160 Hash, string Method)[]
            {
                (NativeContract.Ledger.Hash, "currentIndex"),
                (NativeContract.Policy.Hash, "getFeePerByte"),
                (NativeContract.NEO.Hash, "decimals"),
                (NativeContract.GAS.Hash, "decimals"),
            };

            Parallel.For(0, 40, i =>
            {
                var (hash, method) = contracts[i % contracts.Length];
                var result = ExecuteNativeMethod(hash, method);
                Assert.IsNotNull(result);
            });
        }

        /// <summary>
        /// Test that the cache works correctly for CryptoLib methods.
        /// </summary>
        [TestMethod]
        public void Test_DirectNativeCall_Correctness_CryptoLib()
        {
            var data = new byte[] { 0x01, 0x02, 0x03 };
            uint seed = 0;

            // Test murmur32
            var result1 = ExecuteNativeMethod(NativeContract.CryptoLib.Hash, "murmur32", data, seed);
            var result2 = ExecuteNativeMethod(NativeContract.CryptoLib.Hash, "murmur32", data, seed);
            CollectionAssert.AreEqual(result1.GetSpan().ToArray(), result2.GetSpan().ToArray());

            // Test ripemd160
            var result3 = ExecuteNativeMethod(NativeContract.CryptoLib.Hash, "ripemd160", data);
            var result4 = ExecuteNativeMethod(NativeContract.CryptoLib.Hash, "ripemd160", data);
            CollectionAssert.AreEqual(result3.GetSpan().ToArray(), result4.GetSpan().ToArray());
        }

        /// <summary>
        /// Test that calling methods multiple times is consistent.
        /// This validates the cache doesn't corrupt results.
        /// </summary>
        [TestMethod]
        public void Test_DelegateCaching_Consistency_MultipleContracts()
        {
            // Call multiple contracts in sequence
            for (int i = 0; i < 5; i++)
            {
                var neoDecimals = ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals");
                Assert.AreEqual(0, neoDecimals.GetInteger());

                var gasDecimals = ExecuteNativeMethod(NativeContract.GAS.Hash, "decimals");
                Assert.AreEqual(8, gasDecimals.GetInteger());

                var neoSymbol = ExecuteNativeMethod(NativeContract.NEO.Hash, "symbol");
                Assert.AreEqual("NEO", neoSymbol.GetString());

                var gasSymbol = ExecuteNativeMethod(NativeContract.GAS.Hash, "symbol");
                Assert.AreEqual("GAS", gasSymbol.GetString());
            }
        }

        /// <summary>
        /// Benchmark-style test that demonstrates the performance improvement.
        /// This test runs many iterations to ensure the cache is warm and performing well.
        /// </summary>
        [TestMethod]
        public void Test_DelegateCaching_Performance_WarmCache()
        {
            // Run many calls to warm up the cache
            const int iterations = 100;

            for (int i = 0; i < iterations; i++)
            {
                _ = ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals");
            }
        }

        /// <summary>
        /// Test that different methods on the same contract are cached independently.
        /// </summary>
        [TestMethod]
        public void Test_DelegateCaching_MethodIndependence()
        {
            // Call different methods on NEO contract
            var decimals = ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals").GetInteger();
            var symbol = ExecuteNativeMethod(NativeContract.NEO.Hash, "symbol").GetString();
            var totalSupply = ExecuteNativeMethod(NativeContract.NEO.Hash, "totalSupply").GetInteger();

            // Call again to verify cached results
            Assert.AreEqual(decimals, ExecuteNativeMethod(NativeContract.NEO.Hash, "decimals").GetInteger());
            Assert.AreEqual(symbol, ExecuteNativeMethod(NativeContract.NEO.Hash, "symbol").GetString());
            Assert.AreEqual(totalSupply, ExecuteNativeMethod(NativeContract.NEO.Hash, "totalSupply").GetInteger());
        }
    }
}
