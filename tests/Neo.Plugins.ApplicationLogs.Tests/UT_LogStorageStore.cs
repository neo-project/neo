// Copyright (C) 2015-2025 The Neo Project.
//
// UT_LogStorageStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Persistence.Providers;
using Neo.Plugins.ApplicationLogs;
using Neo.Plugins.ApplicationLogs.Store;
using Neo.Plugins.ApplicationLogs.Store.States;
using Neo.Plugins.ApplicationsLogs.Tests.Setup;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;

namespace Neo.Plugins.ApplicationsLogs.Tests
{
    [TestClass]
    public class UT_LogStorageStore
    {
        [TestMethod]
        [DataRow(TriggerType.OnPersist, "0x0000000000000000000000000000000000000000000000000000000000000001")]
        [DataRow(TriggerType.Application, "0x0000000000000000000000000000000000000000000000000000000000000002")]
        [DataRow(TriggerType.PostPersist, "0x0000000000000000000000000000000000000000000000000000000000000003")]
        public void Test_Put_Get_BlockState_Storage(TriggerType expectedAppTrigger, string expectedBlockHashString)
        {
            var expectedGuid = Guid.NewGuid();
            var expectedHash = UInt256.Parse(expectedBlockHashString);

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    var ok = lss.TryGetBlockState(expectedHash, expectedAppTrigger, out var actualState);
                    Assert.IsFalse(ok);
                    Assert.IsNull(actualState);

                    // Put Block States in Storage for each Trigger
                    lss.PutBlockState(expectedHash, expectedAppTrigger, BlockLogState.Create([expectedGuid]));
                    // Commit Data to "Store" Storage for Lookup
                    snapshot.Commit();
                }
            }

            // The Current way that ISnapshot Works we need to Create New Instance of LogStorageStore
            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                // Get OnPersist Block State from Storage
                var actualFound = lss.TryGetBlockState(expectedHash, expectedAppTrigger, out var actualState);

                Assert.IsTrue(actualFound);
                Assert.IsNotNull(actualState);
                Assert.IsNotNull(actualState.NotifyLogIds);
                Assert.ContainsSingle(actualState.NotifyLogIds);
                Assert.AreEqual(expectedGuid, actualState.NotifyLogIds[0]);
            }
        }

        [TestMethod]
        [DataRow("00000000-0000-0000-0000-000000000000", "0x0000000000000000000000000000000000000000000000000000000000000000")]
        [DataRow("00000000-0000-0000-0000-000000000001", "0x0000000000000000000000000000000000000000000000000000000000000001")]
        public void Test_Put_Get_TransactionEngineState_Storage(string expectedLogId, string expectedHashString)
        {
            var expectedGuid = Guid.Parse(expectedLogId);
            var expectedTxHash = UInt256.Parse(expectedHashString);

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    var ok = lss.TryGetTransactionEngineState(expectedTxHash, out var actualState);
                    Assert.IsFalse(ok);
                    Assert.IsNull(actualState);

                    // Put Block States in Storage for each Trigger
                    lss.PutTransactionEngineState(expectedTxHash, TransactionEngineLogState.Create([expectedGuid]));
                    // Commit Data to "Store" Storage for Lookup
                    snapshot.Commit();
                }
            }

            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                // Get OnPersist Block State from Storage
                var actualFound = lss.TryGetTransactionEngineState(expectedTxHash, out var actualState);

                Assert.IsTrue(actualFound);
                Assert.IsNotNull(actualState);
                Assert.IsNotNull(actualState.LogIds);
                Assert.ContainsSingle(actualState.LogIds);
                Assert.AreEqual(expectedGuid, actualState.LogIds[0]);
            }
        }

        [TestMethod]
        [DataRow("0x0000000000000000000000000000000000000000", "Hello World")]
        [DataRow("0x0000000000000000000000000000000000000001", "Hello Again")]
        public void Test_Put_Get_EngineState_Storage(string expectedScriptHashString, string expectedMessage)
        {
            var expectedScriptHash = UInt160.Parse(expectedScriptHashString);
            var expectedGuid = Guid.Empty;

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    var ok = lss.TryGetEngineState(Guid.NewGuid(), out var actualState);
                    Assert.IsFalse(ok);
                    Assert.IsNull(actualState);

                    expectedGuid = lss.PutEngineState(EngineLogState.Create(expectedScriptHash, expectedMessage));
                    snapshot.Commit();
                }
            }

            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                var actualFound = lss.TryGetEngineState(expectedGuid, out var actualState);

                Assert.IsTrue(actualFound);
                Assert.IsNotNull(actualState);
                Assert.AreEqual(expectedScriptHash, actualState.ScriptHash);
                Assert.AreEqual(expectedMessage, actualState.Message);
            }
        }

        [TestMethod]
        [DataRow("0x0000000000000000000000000000000000000000", "SayHello", "00000000-0000-0000-0000-000000000000")]
        [DataRow("0x0000000000000000000000000000000000000001", "SayGoodBye", "00000000-0000-0000-0000-000000000001")]
        public void Test_Put_Get_NotifyState_Storage(string expectedScriptHashString, string expectedEventName, string expectedItemGuidString)
        {
            var expectedScriptHash = UInt160.Parse(expectedScriptHashString);
            var expectedNotifyEventArgs = new NotifyEventArgs(null, expectedScriptHash, expectedEventName, []);
            var expectedItemGuid = Guid.Parse(expectedItemGuidString);
            var expectedGuid = Guid.Empty;

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    var ok = lss.TryGetNotifyState(Guid.NewGuid(), out var actualState);
                    Assert.IsFalse(ok);
                    Assert.IsNull(actualState);

                    expectedGuid = lss.PutNotifyState(NotifyLogState.Create(expectedNotifyEventArgs, [expectedItemGuid]));
                    snapshot.Commit();
                }
            }

            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                var actualFound = lss.TryGetNotifyState(expectedGuid, out var actualState);

                Assert.IsTrue(actualFound);
                Assert.IsNotNull(actualState);
                Assert.AreEqual(expectedScriptHash, actualState.ScriptHash);
                Assert.AreEqual(expectedEventName, actualState.EventName);
                Assert.IsNotNull(actualState.StackItemIds);
                Assert.ContainsSingle(actualState.StackItemIds);
                Assert.AreEqual(expectedItemGuid, actualState.StackItemIds[0]);
            }
        }

        [TestMethod]
        public void Test_StackItemState()
        {
            using var store = new MemoryStore();
            using var snapshot = store.GetSnapshot();
            using var lss = new LogStorageStore(snapshot);

            // Make sure to initialize Settings.Default.
            using var _ = new LogReader();

            var ok = lss.TryGetStackItemState(Guid.NewGuid(), out var actualState);
            Assert.IsFalse(ok);
            Assert.AreEqual(StackItem.Null, actualState);

            var id1 = lss.PutStackItemState(new Integer(1));
            var id2 = lss.PutStackItemState(new Integer(2));

            snapshot.Commit();

            using var snapshot2 = store.GetSnapshot();
            using var lss2 = new LogStorageStore(snapshot2);
            ok = lss2.TryGetStackItemState(id1, out var actualState1);
            Assert.IsTrue(ok);
            Assert.AreEqual(new Integer(1), actualState1);

            ok = lss2.TryGetStackItemState(id2, out var actualState2);
            Assert.IsTrue(ok);
            Assert.AreEqual(new Integer(2), actualState2);
        }

        [TestMethod]
        public void Test_TransactionState()
        {
            using var store = new MemoryStore();
            using var snapshot = store.GetSnapshot();
            using var lss = new LogStorageStore(snapshot);

            // random 32 bytes
            var bytes = new byte[32];
            Random.Shared.NextBytes(bytes);

            var hash = new UInt256(bytes);
            var ok = lss.TryGetTransactionState(hash, out var actualState);
            Assert.IsFalse(ok);
            Assert.IsNull(actualState);

            var guid = Guid.NewGuid();
            lss.PutTransactionState(hash, TransactionLogState.Create([guid]));
            snapshot.Commit();

            using var snapshot2 = store.GetSnapshot();
            using var lss2 = new LogStorageStore(snapshot2);
            ok = lss2.TryGetTransactionState(hash, out actualState);
            Assert.IsTrue(ok);
            Assert.AreEqual(TransactionLogState.Create([guid]), actualState);
        }

        [TestMethod]
        public void Test_ExecutionState()
        {
            using var store = new MemoryStore();
            using var snapshot = store.GetSnapshot();
            using var lss = new LogStorageStore(snapshot);

            var ok = lss.TryGetExecutionState(Guid.NewGuid(), out var actualState);
            Assert.IsFalse(ok);
            Assert.IsNull(actualState);

            // ExecutionLogState.Serialize
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((byte)VMState.HALT);
            writer.WriteVarString("Test");
            writer.Write(100ul);
            writer.Write(1u);
            writer.WriteVarBytes(Guid.NewGuid().ToByteArray());
            writer.Flush();

            var bytes = stream.ToArray();
            var state = new ExecutionLogState();

            var reader = new MemoryReader(bytes);
            state.Deserialize(ref reader);

            var guid = lss.PutExecutionState(state);
            snapshot.Commit();

            using var snapshot2 = store.GetSnapshot();
            using var lss2 = new LogStorageStore(snapshot2);
            ok = lss2.TryGetExecutionState(guid, out actualState);
            Assert.IsTrue(ok);
            Assert.AreEqual(state, actualState);
        }

        [TestMethod]
        public void Test_ContractState()
        {
            using var store = new MemoryStore();
            using var snapshot = store.GetSnapshot();
            using var lss = new LogStorageStore(snapshot);

            var guid = Guid.NewGuid();
            var scriptHash = UInt160.Parse("0x0000000000000000000000000000000000000000");
            var timestamp = 100ul;
            var index = 1u;

            var ok = lss.TryGetContractState(scriptHash, timestamp, index, out var actualState);
            Assert.IsFalse(ok);
            Assert.IsNull(actualState);

            // random 32 bytes
            var bytes = new byte[32];
            Random.Shared.NextBytes(bytes);

            // ContractLogState.Serialize
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(new UInt256(bytes));
            writer.Write((byte)TriggerType.All);
            writer.Write(scriptHash);
            writer.WriteVarString("Test");
            writer.Write(1u);
            writer.WriteVarBytes(Guid.NewGuid().ToByteArray());
            writer.Flush();

            bytes = stream.ToArray();
            var state = new ContractLogState();
            var reader = new MemoryReader(bytes);
            state.Deserialize(ref reader);

            lss.PutContractState(scriptHash, timestamp, index, state);
            snapshot.Commit();

            using var snapshot2 = store.GetSnapshot();
            using var lss2 = new LogStorageStore(snapshot2);
            ok = lss2.TryGetContractState(scriptHash, timestamp, index, out actualState);
            Assert.IsTrue(ok);
            Assert.AreEqual(state, actualState);
        }
    }
}
