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

using Neo.Extensions;
using Neo.IO;
using Neo.Persistence.Providers;
using Neo.Plugins.ApplicationLogs.Store;
using Neo.Plugins.ApplicationLogs.Store.States;
using Neo.Plugins.ApplicationsLogs.Tests.Setup;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using Xunit;

namespace Neo.Plugins.ApplicationsLogs.Tests
{
    public class UT_LogStorageStore
    {
        [Theory]
        [InlineData(TriggerType.OnPersist, "0x0000000000000000000000000000000000000000000000000000000000000001")]
        [InlineData(TriggerType.Application, "0x0000000000000000000000000000000000000000000000000000000000000002")]
        [InlineData(TriggerType.PostPersist, "0x0000000000000000000000000000000000000000000000000000000000000003")]
        public void Test_Put_Get_BlockState_Storage(TriggerType expectedAppTrigger, string expectedBlockHashString)
        {
            var expectedGuid = Guid.NewGuid();
            var expectedHash = UInt256.Parse(expectedBlockHashString);

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    var ok = lss.TryGetBlockState(expectedHash, expectedAppTrigger, out var actualState);
                    Assert.False(ok);
                    Assert.Null(actualState);

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

                Assert.True(actualFound);
                Assert.NotNull(actualState);
                Assert.NotNull(actualState.NotifyLogIds);
                Assert.Single(actualState.NotifyLogIds);
                Assert.Equal(expectedGuid, actualState.NotifyLogIds[0]);
            }
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000", "0x0000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("00000000-0000-0000-0000-000000000001", "0x0000000000000000000000000000000000000000000000000000000000000001")]
        public void Test_Put_Get_TransactionEngineState_Storage(string expectedLogId, string expectedHashString)
        {
            var expectedGuid = Guid.Parse(expectedLogId);
            var expectedTxHash = UInt256.Parse(expectedHashString);

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    var ok = lss.TryGetTransactionEngineState(expectedTxHash, out var actualState);
                    Assert.False(ok);
                    Assert.Null(actualState);

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

                Assert.True(actualFound);
                Assert.NotNull(actualState);
                Assert.NotNull(actualState.LogIds);
                Assert.Single(actualState.LogIds);
                Assert.Equal(expectedGuid, actualState.LogIds[0]);
            }
        }

        [Theory]
        [InlineData("0x0000000000000000000000000000000000000000", "Hello World")]
        [InlineData("0x0000000000000000000000000000000000000001", "Hello Again")]
        public void Test_Put_Get_EngineState_Storage(string expectedScriptHashString, string expectedMessage)
        {
            var expectedScriptHash = UInt160.Parse(expectedScriptHashString);
            var expectedGuid = Guid.Empty;

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    var ok = lss.TryGetEngineState(Guid.NewGuid(), out var actualState);
                    Assert.False(ok);
                    Assert.Null(actualState);

                    expectedGuid = lss.PutEngineState(EngineLogState.Create(expectedScriptHash, expectedMessage));
                    snapshot.Commit();
                }
            }

            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                var actualFound = lss.TryGetEngineState(expectedGuid, out var actualState);

                Assert.True(actualFound);
                Assert.NotNull(actualState);
                Assert.Equal(expectedScriptHash, actualState.ScriptHash);
                Assert.Equal(expectedMessage, actualState.Message);
            }
        }

        [Theory]
        [InlineData("0x0000000000000000000000000000000000000000", "SayHello", "00000000-0000-0000-0000-000000000000")]
        [InlineData("0x0000000000000000000000000000000000000001", "SayGoodBye", "00000000-0000-0000-0000-000000000001")]
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
                    Assert.False(ok);
                    Assert.Null(actualState);

                    expectedGuid = lss.PutNotifyState(NotifyLogState.Create(expectedNotifyEventArgs, [expectedItemGuid]));
                    snapshot.Commit();
                }
            }

            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                var actualFound = lss.TryGetNotifyState(expectedGuid, out var actualState);

                Assert.True(actualFound);
                Assert.NotNull(actualState);
                Assert.Equal(expectedScriptHash, actualState.ScriptHash);
                Assert.Equal(expectedEventName, actualState.EventName);
                Assert.NotNull(actualState.StackItemIds);
                Assert.Single(actualState.StackItemIds);
                Assert.Equal(expectedItemGuid, actualState.StackItemIds[0]);
            }
        }

        [Fact]
        public void Test_StackItemState()
        {
            using var store = new MemoryStore();
            using var snapshot = store.GetSnapshot();
            using var lss = new LogStorageStore(snapshot);

            var ok = lss.TryGetStackItemState(Guid.NewGuid(), out var actualState);
            Assert.False(ok);
            Assert.Equal(StackItem.Null, actualState);

            var id1 = lss.PutStackItemState(new Integer(1));
            var id2 = lss.PutStackItemState(new Integer(2));

            snapshot.Commit();

            using var snapshot2 = store.GetSnapshot();
            using var lss2 = new LogStorageStore(snapshot2);
            ok = lss2.TryGetStackItemState(id1, out var actualState1);
            Assert.True(ok);
            Assert.Equal(new Integer(1), actualState1);

            ok = lss2.TryGetStackItemState(id2, out var actualState2);
            Assert.True(ok);
            Assert.Equal(new Integer(2), actualState2);
        }

        [Fact]
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
            Assert.False(ok);
            Assert.Null(actualState);

            var guid = Guid.NewGuid();
            lss.PutTransactionState(hash, TransactionLogState.Create([guid]));
            snapshot.Commit();

            using var snapshot2 = store.GetSnapshot();
            using var lss2 = new LogStorageStore(snapshot2);
            ok = lss2.TryGetTransactionState(hash, out actualState);
            Assert.True(ok);
            Assert.Equal(TransactionLogState.Create([guid]), actualState);
        }

        [Fact]
        public void Test_ExecutionState()
        {
            using var store = new MemoryStore();
            using var snapshot = store.GetSnapshot();
            using var lss = new LogStorageStore(snapshot);

            var ok = lss.TryGetExecutionState(Guid.NewGuid(), out var actualState);
            Assert.False(ok);
            Assert.Null(actualState);

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
            Assert.True(ok);
            Assert.Equal(state, actualState);
        }

        [Fact]
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
            Assert.False(ok);
            Assert.Null(actualState);

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
            Assert.True(ok);
            Assert.Equal(state, actualState);
        }
    }
}
