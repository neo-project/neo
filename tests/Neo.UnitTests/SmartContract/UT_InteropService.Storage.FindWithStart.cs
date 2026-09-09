// Copyright (C) 2015-2026 The Neo Project.
//
// UT_InteropService.Storage.FindWithStart.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_InteropService
    {
        [TestMethod]
        [DataRow("23", false, "23,28,34")]
        [DataRow("23", true, "23,10,")]
        [DataRow("27", false, "28,34")]
        [DataRow("27", true, "23,10,")]
        [DataRow("00", false, "10,23,28,34")]
        [DataRow("00", true, "")]
        [DataRow("99", false, null)]
        [DataRow("99", true, "34,28,23,10,")]
        [DataRow("", false, ",10,23,28,34")]
        [DataRow("", true, "")]
        public void TestStorage_FindWithStart(string start, bool backwards, string expected)
        {
            var snapshot = _snapshotCache.CloneCache();
            foreach (var id in new[] { 123, 124 })
                foreach (var key in new[] { "@", "A_", "A_10", "A_23", "A_28", "A_34", "B" })
                    snapshot.Add(new StorageKey { Id = id, Key = Encoding.UTF8.GetBytes(key) }, new StorageItem([1]));

            // Exercise both parent-cache entries and changes in the current snapshot.
            var child = snapshot.CloneCache();
            child.Delete(new StorageKey { Id = 123, Key = "A_28"u8.ToArray() });
            child.Add(new StorageKey { Id = 123, Key = "A_28"u8.ToArray() }, new StorageItem([2]));
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, child);
            var options = FindOptions.KeysOnly | FindOptions.RemovePrefix;
            if (backwards) options |= FindOptions.Backwards;
            using var iterator = engine.FindWithStart(new StorageContext { Id = 123, IsReadOnly = true }, "A_"u8.ToArray(), Encoding.UTF8.GetBytes(start), options);
            var actual = new List<string>();
            while (iterator.Next()) actual.Add(Encoding.UTF8.GetString(iterator.Value().GetSpan()));
            Assert.AreSequenceEqual(expected == null ? [] : expected.Split(','), [.. actual]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TestStorage_FindWithStartEmptyPrefixAndBinaryKeys(bool backwards)
        {
            var snapshot = _snapshotCache.CloneCache();
            foreach (var id in new[] { 123, 124 })
                foreach (var key in new byte[][] { [], [0], [0xff], [0xff, 0xff] })
                    snapshot.Add(new StorageKey { Id = id, Key = key }, new StorageItem([1]));
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            using var iterator = engine.FindWithStart(new StorageContext { Id = 123 }, [], [0xff], FindOptions.KeysOnly | (backwards ? FindOptions.Backwards : FindOptions.None));
            var actual = new List<string>();
            while (iterator.Next()) actual.Add(Convert.ToHexString(iterator.Value().GetSpan()));
            Assert.AreSequenceEqual(backwards ? new[] { "FF", "00", "" } : ["FF", "FFFF"], [.. actual]);

            using var binaryPrefix = engine.FindWithStart(new StorageContext { Id = 123 }, [0xff], [0xff], FindOptions.KeysOnly | FindOptions.RemovePrefix | (backwards ? FindOptions.Backwards : FindOptions.None));
            Assert.IsTrue(binaryPrefix.Next());
            Assert.AreSequenceEqual(new byte[] { 0xff }, binaryPrefix.Value().GetSpan().ToArray());
            Assert.AreEqual(backwards, binaryPrefix.Next());
            if (backwards) Assert.HasCount(0, binaryPrefix.Value().GetSpan());
            Assert.IsFalse(binaryPrefix.Next());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TestStorage_FindWithStartMissingPrefix(bool backwards)
        {
            var snapshot = _snapshotCache.CloneCache();
            foreach (var key in new[] { "A", "C" })
                snapshot.Add(new StorageKey { Id = 123, Key = Encoding.UTF8.GetBytes(key) }, new StorageItem([1]));
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            using var iterator = engine.FindWithStart(new StorageContext { Id = 123 }, "B"u8.ToArray(), [], backwards ? FindOptions.Backwards : FindOptions.None);
            Assert.IsFalse(iterator.Next());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TestStorage_FindWithStartCacheChanges(bool backwards)
        {
            var snapshot = _snapshotCache.CloneCache();
            foreach (var suffix in new[] { "10", "23", "28", "34" })
                snapshot.Add(new StorageKey { Id = 123, Key = Encoding.UTF8.GetBytes("A_" + suffix) }, new StorageItem([1]));
            var child = snapshot.CloneCache();
            child.Delete(new StorageKey { Id = 123, Key = "A_23"u8.ToArray() });
            child.GetAndChange(new StorageKey { Id = 123, Key = "A_28"u8.ToArray() }).Value = new byte[] { 28 };
            child.Add(new StorageKey { Id = 123, Key = "A_27"u8.ToArray() }, new StorageItem([27]));
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, child);
            using var iterator = engine.FindWithStart(new StorageContext { Id = 123 }, "A_"u8.ToArray(), "23"u8.ToArray(), FindOptions.ValuesOnly | (backwards ? FindOptions.Backwards : FindOptions.None));
            var actual = new List<byte>();
            while (iterator.Next()) actual.Add(iterator.Value().GetSpan()[0]);
            Assert.AreSequenceEqual(backwards ? new byte[] { 1 } : [27, 28, 1], [.. actual]);
        }

        [TestMethod]
        public void TestStorage_FindWithStartOptions()
        {
            var snapshot = _snapshotCache.CloneCache();
            var value = BinarySerializer.Serialize(new Struct { 11, 22 }, ExecutionEngineLimits.Default);
            snapshot.Add(new StorageKey { Id = 123, Key = "A_23"u8.ToArray() }, new StorageItem(value));
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            var context = new StorageContext { Id = 123 };
            // Every valid option combination must preserve the existing Find result shape;
            // every invalid combination must fail with the same exception.
            for (var bits = 0; bits <= byte.MaxValue; bits++)
            {
                var options = (FindOptions)bits;
                IIterator existing = null;
                Type errorType = null;
                try
                {
                    existing = engine.Find(context, "A_"u8.ToArray(), options);
                }
                catch (ArgumentException error)
                {
                    errorType = error.GetType();
                }
                if (errorType != null)
                {
                    var actual = Assert.Throws<ArgumentException>(() => engine.FindWithStart(context, "A_"u8.ToArray(), "23"u8.ToArray(), options));
                    Assert.AreEqual(errorType, actual.GetType());
                    continue;
                }
                using (existing)
                using (var iterator = engine.FindWithStart(context, "A_"u8.ToArray(), "23"u8.ToArray(), options))
                {
                    Assert.IsTrue(existing.Next());
                    Assert.IsTrue(iterator.Next());
                    Assert.AreSequenceEqual(BinarySerializer.Serialize(existing.Value(), ExecutionEngineLimits.Default), BinarySerializer.Serialize(iterator.Value(), ExecutionEngineLimits.Default));
                    Assert.IsFalse(iterator.Next());
                }
            }
        }

        [TestMethod]
        [DataRow(true, true, true, false, VMState.HALT)]
        [DataRow(true, true, true, true, VMState.HALT)]
        [DataRow(false, true, true, false, VMState.FAULT)]
        [DataRow(true, false, true, false, VMState.FAULT)]
        [DataRow(true, true, false, false, VMState.FAULT)]
        public void TestStorage_LocalFindWithStartSyscall(bool enabled, bool readStates, bool deployed, bool backwards, VMState expectedState)
        {
            var snapshot = _snapshotCache.CloneCache();
            var contract = TestUtils.GetContract();
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Storage_Local_FindWithStart);
            var settings = TestProtocolSettings.Default;
            if (!enabled) settings = settings with { Hardforks = settings.Hardforks.Remove(Hardfork.HF_Iara) };
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings);
            engine.LoadScript(script.ToArray(), configureState: state => state.CallFlags = readStates ? CallFlags.ReadStates : CallFlags.None);
            if (deployed) snapshot.AddContract(engine.CurrentScriptHash, contract);
            foreach (var id in new[] { contract.Id, contract.Id + 1 })
                foreach (var suffix in new[] { "23", "28" })
                    snapshot.Add(new StorageKey { Id = id, Key = Encoding.UTF8.GetBytes("A_" + suffix) }, new StorageItem([42]));
            engine.Push((int)(FindOptions.KeysOnly | FindOptions.RemovePrefix | (backwards ? FindOptions.Backwards : FindOptions.None)));
            engine.Push("27"u8.ToArray());
            engine.Push("A_"u8.ToArray());
            Assert.AreEqual(expectedState, engine.Execute());
            if (expectedState == VMState.HALT)
            {
                using var iterator = engine.ResultStack.Pop().GetInterface<IIterator>();
                Assert.IsTrue(iterator.Next());
                Assert.AreEqual(backwards ? "23" : "28", Encoding.UTF8.GetString(iterator.Value().GetSpan()));
                Assert.IsFalse(iterator.Next());
            }
        }

        [TestMethod]
        [DataRow(true, true, VMState.HALT)]
        [DataRow(false, true, VMState.FAULT)]
        [DataRow(true, false, VMState.FAULT)]
        public void TestStorage_FindWithStartSyscall(bool enabled, bool readStates, VMState expectedState)
        {
            var snapshot = _snapshotCache.CloneCache();
            snapshot.Add(new StorageKey { Id = 123, Key = "A_28"u8.ToArray() }, new StorageItem([42]));
            var settings = TestProtocolSettings.Default;
            if (!enabled) settings = settings with { Hardforks = settings.Hardforks.Remove(Hardfork.HF_Iara) };
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings);
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Storage_FindWithStart);
            engine.LoadScript(script.ToArray(), configureState: state => state.CallFlags = readStates ? CallFlags.ReadStates : CallFlags.None);
            engine.Push((int)FindOptions.ValuesOnly);
            engine.Push("27"u8.ToArray());
            engine.Push("A_"u8.ToArray());
            engine.Push(new InteropInterface(new StorageContext { Id = 123, IsReadOnly = true }));
            Assert.AreEqual(expectedState, engine.Execute());
            if (expectedState == VMState.HALT)
            {
                using var iterator = engine.ResultStack.Pop().GetInterface<IIterator>();
                Assert.IsTrue(iterator.Next());
                Assert.AreSequenceEqual("*"u8.ToArray(), iterator.Value().GetSpan().ToArray());
                Assert.IsFalse(iterator.Next());
            }
        }
    }
}
