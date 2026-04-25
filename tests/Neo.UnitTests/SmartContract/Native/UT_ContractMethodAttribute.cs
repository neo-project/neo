// Copyright (C) 2015-2026 The Neo Project.
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
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        // Custom class to test parameter type restriction: implements IReadOnlyStore but is not DataCache
        // This should NOT be accepted as a parameter type - only IReadOnlyStore interface itself or DataCache are allowed
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
            // This test verifies that CustomReadOnlyStore (a custom implementation of IReadOnlyStore)
            // is NOT accepted as a parameter type. Only IReadOnlyStore interface itself or DataCache
            // (and its subclasses) are allowed.
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var testType = typeof(TestBugDetection);

            var method = testType.GetMethod(nameof(TestBugDetection.MethodWithCustomReadOnlyStore), flags);
            var metadata = new ContractMethodMetadata(method!, new ContractMethodAttribute());

            // CustomReadOnlyStore should NOT be accepted, even though it implements IReadOnlyStore
            // Only IReadOnlyStore interface itself or DataCache are allowed
            Assert.IsFalse(metadata.NeedSnapshot,
                "CustomReadOnlyStore (implements IReadOnlyStore) should NOT set NeedSnapshot to true. " +
                "Only IReadOnlyStore interface itself or DataCache are allowed as parameter types.");
            Assert.IsFalse(metadata.NeedApplicationEngine,
                "CustomReadOnlyStore should not set NeedApplicationEngine to true");
            // Since NeedSnapshot is false, the parameter should not be skipped
            Assert.AreEqual(2, metadata.Parameters.Length,
                "CustomReadOnlyStore parameter should not be skipped, leaving both CustomReadOnlyStore and UInt160");
            Assert.AreEqual(typeof(UInt160), metadata.Parameters[1].Type,
                "Second parameter should be UInt160");
        }

        private sealed class InvokeTestEngine(DataCache snapshot) : ApplicationEngine(TriggerType.Application, null, snapshot, null, TestProtocolSettings.Default, 0)
        {
        }

        private sealed class InvokeTestNativeContract : NativeContract
        {
            public ApplicationEngine SeenEngine { get; private set; }
            public IReadOnlyStore SeenSnapshot { get; private set; }

            [ContractMethod]
            private int MethodUsingEngine(ApplicationEngine engine, int value)
            {
                SeenEngine = engine;
                return value + 1;
            }

            [ContractMethod]
            private bool MethodUsingSnapshot(IReadOnlyStore snapshot)
            {
                SeenSnapshot = snapshot;
                return snapshot is not null;
            }

            [ContractMethod]
            private static int MethodUsingParametersOnly(int left, int right)
            {
                return left + right;
            }

            [ContractMethod]
            private static void MethodThrowing()
            {
                throw new InvalidOperationException();
            }
        }

        private static readonly InvokeTestNativeContract s_invokeContract = (InvokeTestNativeContract)RuntimeHelpers.GetUninitializedObject(typeof(InvokeTestNativeContract));

        [TestMethod]
        public void TestInvokePassesApplicationEngine()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = new InvokeTestEngine(snapshot);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var method = typeof(InvokeTestNativeContract).GetMethod("MethodUsingEngine", flags);
            var metadata = new ContractMethodMetadata(method!, new ContractMethodAttribute());

            var result = metadata.Invoke(s_invokeContract, engine, [41]);

            Assert.AreEqual(42, result);
            Assert.AreSame(engine, s_invokeContract.SeenEngine);
        }

        [TestMethod]
        public void TestInvokePassesSnapshot()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = new InvokeTestEngine(snapshot);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var method = typeof(InvokeTestNativeContract).GetMethod("MethodUsingSnapshot", flags);
            var metadata = new ContractMethodMetadata(method!, new ContractMethodAttribute());

            var result = metadata.Invoke(s_invokeContract, engine, []);

            Assert.AreEqual(true, result);
            Assert.AreSame(engine.SnapshotCache, s_invokeContract.SeenSnapshot);
        }

        [TestMethod]
        public void TestInvokePassesPublicParametersOnly()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = new InvokeTestEngine(snapshot);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var method = typeof(InvokeTestNativeContract).GetMethod("MethodUsingParametersOnly", flags);
            var metadata = new ContractMethodMetadata(method!, new ContractMethodAttribute());

            var result = metadata.Invoke(s_invokeContract, engine, [2, 5]);

            Assert.AreEqual(7, result);
        }

        [TestMethod]
        public void TestInvokeWrapsTargetException()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = new InvokeTestEngine(snapshot);
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var method = typeof(InvokeTestNativeContract).GetMethod("MethodThrowing", flags);
            var metadata = new ContractMethodMetadata(method!, new ContractMethodAttribute());

            var exception = Assert.ThrowsExactly<TargetInvocationException>(() => metadata.Invoke(s_invokeContract, engine, []));

            Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        }
    }
}
