// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ExecutionContextState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ExecutionContextState
    {
        [TestMethod]
        public void Defaults_And_PropertyAssignment()
        {
            var state = new ExecutionContextState();
            Assert.AreEqual(CallFlags.All, state.CallFlags);
            Assert.IsFalse(state.WhiteListed);
            Assert.IsFalse(state.IsDynamicCall);
            Assert.AreEqual(0, state.NotificationCount);
            Assert.IsNull(state.ScriptHash);
            Assert.IsNull(state.CallingContext);
            Assert.IsNull(state.Contract);
            Assert.IsNull(state.SnapshotCache);

            using var store = new MemoryStore();
            using var cache = new StoreCache(store);
            state.ScriptHash = UInt160.Zero;
            state.CallFlags = CallFlags.ReadOnly;
            state.SnapshotCache = cache;
            state.NotificationCount = 3;
            state.IsDynamicCall = true;
            state.WhiteListed = true;
            state.NativeCallingScriptHash = UInt160.Parse("0x0000000000000000000000000000000000000001");

            Assert.AreEqual(UInt160.Zero, state.ScriptHash);
            Assert.AreEqual(CallFlags.ReadOnly, state.CallFlags);
            Assert.AreSame(cache, state.SnapshotCache);
            Assert.AreEqual(3, state.NotificationCount);
            Assert.IsTrue(state.IsDynamicCall);
            Assert.IsTrue(state.WhiteListed);
            Assert.AreEqual(UInt160.Parse("0x0000000000000000000000000000000000000001"), state.NativeCallingScriptHash);
        }

        [TestMethod]
        public void Obsolete_Snapshot_Aliases_SnapshotCache()
        {
            var state = new ExecutionContextState();
            using var store = new MemoryStore();
            using var cache = new StoreCache(store);
            state.SnapshotCache = cache;
#pragma warning disable CS0618
            Assert.AreSame(cache, state.Snapshot);
#pragma warning restore CS0618
        }
    }
}
