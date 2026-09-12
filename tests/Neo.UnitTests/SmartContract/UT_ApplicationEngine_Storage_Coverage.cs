// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_Storage_Coverage.cs file belongs to the neo project and is free
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
using Neo.VM;
using System;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine_Storage_Coverage
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void GetStorageContext_WithoutDeployedContract_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.GetStorageContext());
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.GetReadOnlyContext());
        }

        [TestMethod]
        public void AsReadOnly_MarksContextReadOnly()
        {
            var ctx = new StorageContext { Id = 1, IsReadOnly = false };
            var ro = ApplicationEngine.AsReadOnly(ctx);
            Assert.IsTrue(ro.IsReadOnly);
            Assert.AreEqual(1, ro.Id);

            var already = new StorageContext { Id = 2, IsReadOnly = true };
            Assert.IsTrue(ApplicationEngine.AsReadOnly(already).IsReadOnly);
        }

        [TestMethod]
        public void Put_ReadOnlyContext_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, gas: 100_0000_0000);
            var ctx = new StorageContext { Id = -1, IsReadOnly = true };
            Assert.ThrowsExactly<ArgumentException>(() => engine.Put(ctx, [1], [2]));
        }

        [TestMethod]
        public void Put_KeyOrValueTooLarge_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, gas: 100_0000_0000);
            var ctx = new StorageContext { Id = -1, IsReadOnly = false };
            var bigKey = new byte[ApplicationEngine.MaxStorageKeySize + 1];
            Assert.ThrowsExactly<ArgumentException>(() => engine.Put(ctx, bigKey, [1]));

            var bigValue = new byte[ApplicationEngine.MaxStorageValueSize + 1];
            Assert.ThrowsExactly<ArgumentException>(() => engine.Put(ctx, [1], bigValue));
        }

        [TestMethod]
        public void Put_Get_Delete_RoundTrip()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, gas: 100_0000_0000);
            var ctx = new StorageContext { Id = -1, IsReadOnly = false };
            engine.Put(ctx, [0xAB], [1, 2, 3]);
            var value = engine.Get(ctx, [0xAB]);
            Assert.IsTrue(value.HasValue);
            Assert.IsTrue(new byte[] { 1, 2, 3 }.AsSpan().SequenceEqual(value.Value.Span));

            engine.Delete(ctx, [0xAB]);
            Assert.IsFalse(engine.Get(ctx, [0xAB]).HasValue);
        }

        [TestMethod]
        public void Find_InvalidOptions_Throw()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            var ctx = new StorageContext { Id = -1, IsReadOnly = true };

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                engine.Find(ctx, [], (FindOptions)0xFF));

            Assert.ThrowsExactly<ArgumentException>(() =>
                engine.Find(ctx, [], FindOptions.KeysOnly | FindOptions.ValuesOnly));

            Assert.ThrowsExactly<ArgumentException>(() =>
                engine.Find(ctx, [], FindOptions.ValuesOnly | FindOptions.RemovePrefix));

            Assert.ThrowsExactly<ArgumentException>(() =>
                engine.Find(ctx, [], FindOptions.PickField0 | FindOptions.PickField1));

            Assert.ThrowsExactly<ArgumentException>(() =>
                engine.Find(ctx, [], FindOptions.PickField0));
        }

        [TestMethod]
        public void Find_ValidOptions_ReturnsIterator()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, gas: 100_0000_0000);
            var write = new StorageContext { Id = -1, IsReadOnly = false };
            engine.Put(write, [0x01], [0x11]);
            engine.Put(write, [0x02], [0x22]);

            var read = new StorageContext { Id = -1, IsReadOnly = true };
            using var it = engine.Find(read, [], FindOptions.None);
            Assert.IsNotNull(it);
            Assert.IsTrue(it.Next());
        }
    }
}
