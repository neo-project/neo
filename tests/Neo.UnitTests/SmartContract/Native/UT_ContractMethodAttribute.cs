// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ContractMethodAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Reflection;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_ContractMethodAttribute
    {
        [TestMethod]
        public void TestConstructorOneArg()
        {
            var arg = new ContractMethodAttribute();

            Assert.IsNull(arg.ActiveIn);

            arg = new ContractMethodAttribute(Hardfork.HF_Aspidochelone);

            Assert.AreEqual(Hardfork.HF_Aspidochelone, arg.ActiveIn);
        }

        class NeedSnapshot
        {
            [ContractMethod]
            public bool MethodReadOnlyStoreView(IReadOnlyStore view) => view is null;

            [ContractMethod]
            public bool MethodDataCache(DataCache dataCache) => dataCache is null;
        }

        class NoNeedSnapshot
        {
            [ContractMethod]
            public bool MethodTwo(ApplicationEngine engine, UInt160 account)
                => engine is null || account is null;

            [ContractMethod]
            public bool MethodOne(ApplicationEngine engine) => engine is null;
        }

        [TestMethod]
        public void TestNeedSnapshot()
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            foreach (var member in typeof(NeedSnapshot).GetMembers(flags))
            {
                foreach (var attribute in member.GetCustomAttributes<ContractMethodAttribute>())
                {
                    var metadata = new ContractMethodMetadata(member, attribute);
                    Assert.IsTrue(metadata.NeedSnapshot);
                }
            }

            foreach (var member in typeof(NoNeedSnapshot).GetMembers(flags))
            {
                foreach (var attribute in member.GetCustomAttributes<ContractMethodAttribute>())
                {
                    var metadata = new ContractMethodMetadata(member, attribute);
                    Assert.IsFalse(metadata.NeedSnapshot);
                    Assert.IsTrue(metadata.NeedApplicationEngine);
                }
            }
        }

        class TestParameterDetection
        {
            [ContractMethod]
            public void MethodWithIReadOnlyStore(IReadOnlyStore snapshot, UInt160 account) { }

            [ContractMethod]
            public void MethodWithDataCache(DataCache snapshot, UInt160 account) { }

            [ContractMethod]
            public void MethodWithApplicationEngine(ApplicationEngine engine, UInt160 account) { }

            [ContractMethod]
            public void MethodWithNormalParameter(UInt160 account) { }

            [ContractMethod]
            public void MethodWithIReadOnlyStoreOnly(IReadOnlyStore snapshot) { }

            [ContractMethod]
            public void MethodWithDataCacheOnly(DataCache snapshot) { }
        }

        // Custom class to test bug detection: implements IReadOnlyStore but is not DataCache
        class CustomReadOnlyStore : IReadOnlyStore
        {
            public Neo.SmartContract.StorageItem this[Neo.SmartContract.StorageKey key] => throw new System.NotImplementedException();
            public bool Contains(Neo.SmartContract.StorageKey key) => throw new System.NotImplementedException();
            public System.Collections.Generic.IEnumerable<(Neo.SmartContract.StorageKey Key, Neo.SmartContract.StorageItem Value)> Find(Neo.SmartContract.StorageKey key_prefix = null, Neo.Persistence.SeekDirection direction = Neo.Persistence.SeekDirection.Forward) => throw new System.NotImplementedException();
            public Neo.SmartContract.StorageItem TryGet(Neo.SmartContract.StorageKey key) => null;
            public bool TryGet(Neo.SmartContract.StorageKey key, out Neo.SmartContract.StorageItem value)
            {
                value = null;
                return false;
            }
        }

        class TestBugDetection
        {
            [ContractMethod]
            public void MethodWithCustomReadOnlyStore(CustomReadOnlyStore snapshot, UInt160 account) { }
        }

        [TestMethod]
        public void TestParameterDetectionAndSkipping()
        {
            // This test specifically verifies the fix for the bug where IsAssignableFrom.
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var testType = typeof(TestParameterDetection);

            // Test IReadOnlyStore parameter detection
            var methodIReadOnlyStore = testType.GetMethod(nameof(TestParameterDetection.MethodWithIReadOnlyStore), flags);
            var metadataIReadOnlyStore = new ContractMethodMetadata(methodIReadOnlyStore!, new ContractMethodAttribute());
            Assert.IsTrue(metadataIReadOnlyStore.NeedSnapshot, "IReadOnlyStore parameter should set NeedSnapshot to true");
            Assert.IsFalse(metadataIReadOnlyStore.NeedApplicationEngine, "IReadOnlyStore parameter should not set NeedApplicationEngine to true");
            Assert.AreEqual(1, metadataIReadOnlyStore.Parameters.Length, "IReadOnlyStore parameter should be skipped, leaving only UInt160");
            Assert.AreEqual(typeof(UInt160), metadataIReadOnlyStore.Parameters[0].Type, "Remaining parameter should be UInt160");

            // Test DataCache parameter detection
            var methodDataCache = testType.GetMethod(nameof(TestParameterDetection.MethodWithDataCache), flags);
            var metadataDataCache = new ContractMethodMetadata(methodDataCache!, new ContractMethodAttribute());
            Assert.IsTrue(metadataDataCache.NeedSnapshot, "DataCache parameter should set NeedSnapshot to true");
            Assert.IsFalse(metadataDataCache.NeedApplicationEngine, "DataCache parameter should not set NeedApplicationEngine to true");
            Assert.AreEqual(1, metadataDataCache.Parameters.Length, "DataCache parameter should be skipped, leaving only UInt160");
            Assert.AreEqual(typeof(UInt160), metadataDataCache.Parameters[0].Type, "Remaining parameter should be UInt160");

            // Test ApplicationEngine parameter detection
            var methodApplicationEngine = testType.GetMethod(nameof(TestParameterDetection.MethodWithApplicationEngine), flags);
            var metadataApplicationEngine = new ContractMethodMetadata(methodApplicationEngine!, new ContractMethodAttribute());
            Assert.IsTrue(metadataApplicationEngine.NeedApplicationEngine, "ApplicationEngine parameter should set NeedApplicationEngine to true");
            Assert.IsFalse(metadataApplicationEngine.NeedSnapshot, "ApplicationEngine parameter should not set NeedSnapshot to true");
            Assert.AreEqual(1, metadataApplicationEngine.Parameters.Length, "ApplicationEngine parameter should be skipped, leaving only UInt160");
            Assert.AreEqual(typeof(UInt160), metadataApplicationEngine.Parameters[0].Type, "Remaining parameter should be UInt160");

            // Test normal parameter (no special parameter)
            var methodNormal = testType.GetMethod(nameof(TestParameterDetection.MethodWithNormalParameter), flags);
            var metadataNormal = new ContractMethodMetadata(methodNormal!, new ContractMethodAttribute());
            Assert.IsFalse(metadataNormal.NeedSnapshot, "Normal parameter should not set NeedSnapshot to true");
            Assert.IsFalse(metadataNormal.NeedApplicationEngine, "Normal parameter should not set NeedApplicationEngine to true");
            Assert.AreEqual(1, metadataNormal.Parameters.Length, "Normal parameter should not be skipped");
            Assert.AreEqual(typeof(UInt160), metadataNormal.Parameters[0].Type, "Parameter should be UInt160");

            // Test IReadOnlyStore only (no other parameters)
            var methodIReadOnlyStoreOnly = testType.GetMethod(nameof(TestParameterDetection.MethodWithIReadOnlyStoreOnly), flags);
            var metadataIReadOnlyStoreOnly = new ContractMethodMetadata(methodIReadOnlyStoreOnly!, new ContractMethodAttribute());
            Assert.IsTrue(metadataIReadOnlyStoreOnly.NeedSnapshot, "IReadOnlyStore parameter should set NeedSnapshot to true");
            Assert.AreEqual(0, metadataIReadOnlyStoreOnly.Parameters.Length, "IReadOnlyStore parameter should be skipped, leaving no parameters");

            // Test DataCache only (no other parameters)
            var methodDataCacheOnly = testType.GetMethod(nameof(TestParameterDetection.MethodWithDataCacheOnly), flags);
            var metadataDataCacheOnly = new ContractMethodMetadata(methodDataCacheOnly!, new ContractMethodAttribute());
            Assert.IsTrue(metadataDataCacheOnly.NeedSnapshot, "DataCache parameter should set NeedSnapshot to true");
            Assert.AreEqual(0, metadataDataCacheOnly.Parameters.Length, "DataCache parameter should be skipped, leaving no parameters");
        }

        [TestMethod]
        public void TestBugDetectionWithCustomReadOnlyStore()
        {
            // This test specifically detects the bug where IsAssignableFrom direction is wrong.
            // With fixed code: typeof(DataCache).IsAssignableFrom(parameterInfos[0].ParameterType) ||
            //                  typeof(IReadOnlyStore).IsAssignableFrom(parameterInfos[0].ParameterType)
            //   typeof(IReadOnlyStore).IsAssignableFrom(typeof(CustomReadOnlyStore)) returns TRUE
            //   So NeedSnapshot would be TRUE (correct!)
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var testType = typeof(TestBugDetection);

            var method = testType.GetMethod(nameof(TestBugDetection.MethodWithCustomReadOnlyStore), flags);
            var metadata = new ContractMethodMetadata(method!, new ContractMethodAttribute());

            // With the bug, this would fail because:
            // CustomReadOnlyStore.IsAssignableFrom(typeof(DataCache)) = false
            // So NeedSnapshot would be false, but it should be true
            Assert.IsTrue(metadata.NeedSnapshot,
                "CustomReadOnlyStore (implements IReadOnlyStore) should set NeedSnapshot to true. " +
                "This test will fail with bug code where IsAssignableFrom direction is wrong.");
            Assert.IsFalse(metadata.NeedApplicationEngine,
                "CustomReadOnlyStore should not set NeedApplicationEngine to true");
            Assert.AreEqual(1, metadata.Parameters.Length,
                "CustomReadOnlyStore parameter should be skipped, leaving only UInt160");
            Assert.AreEqual(typeof(UInt160), metadata.Parameters[0].Type,
                "Remaining parameter should be UInt160");
        }
    }
}
