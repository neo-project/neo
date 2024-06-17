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
        public void Test_Put_Get_BlockState_Storage(TriggerType trigger, string blockHash)
        {
            var expectedGuid = Guid.NewGuid();
            var expectedHash = UInt256.Parse(blockHash);

            using (var snapshot = TestStorage.Store.GetSnapshot())
            {
                using (var lss = new LogStorageStore(snapshot))
                {
                    // Put Block States in Storage for each Trigger
                    lss.PutBlockState(expectedHash, trigger, BlockLogState.Create([expectedGuid]));
                    // Commit Data to "Store" Storage for Lookup
                    snapshot.Commit();
                }
            }

            // The Current way that ISnapshot Works we need to Create New Instance of LogStorageStore
            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                // Get OnPersist Block State from Storage
                lss.TryGetBlockState(expectedHash, trigger, out var actualState);

                Assert.NotNull(actualState);
                Assert.NotNull(actualState.NotifyLogIds);
                Assert.Single(actualState.NotifyLogIds);
                Assert.Equal(expectedGuid, actualState.NotifyLogIds[0]);
            }
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000", "0x0000000000000000000000000000000000000000000000000000000000000001")]
        [InsertTransactionEngineState("00000000-0000-0000-0000-000000000000", "0x0000000000000000000000000000000000000000000000000000000000000001")]
        public void Test_Put_Get_TransactionEngineState_Storage(string expectedLogId, string expectedHashString)
        {
            var expectedGuid = Guid.Parse(expectedLogId);
            var expectedHash = UInt256.Parse(expectedHashString);

            using (var lss = new LogStorageStore(TestStorage.Store.GetSnapshot()))
            {
                // Get OnPersist Block State from Storage
                lss.TryGetTransactionEngineState(expectedHash, out var actualState);

                Assert.NotNull(actualState);
                Assert.NotNull(actualState.LogIds);
                Assert.Single(actualState.LogIds);
                Assert.Equal(expectedGuid, actualState.LogIds[0]);
            }
        }
    }
}
