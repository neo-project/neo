// Copyright (C) 2015-2024 The Neo Project.
//
// UT_LogStorageStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Authorization;
using Neo.Persistence;
using Neo.Plugins.ApplicationLogs.Store;
using Neo.Plugins.ApplicationLogs.Store.States;
using Neo.Plugins.ApplicationsLogs.Tests.Setup;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                Assert.Equal(Guid.Empty, actualState.StackItemIds[0]);
            }
        }
    }
}
