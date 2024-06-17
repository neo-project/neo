// Copyright (C) 2015-2024 The Neo Project.
//
// PutTestTransactionEngineState.cs file belongs to the neo project and is free
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Neo.Plugins.ApplicationsLogs.Tests.Setup
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class InsertTransactionEngineState : BeforeAfterTestAttribute
    {
        private readonly ISnapshot _snapshot;
        private readonly Guid _expectedGuid;
        private readonly UInt256 _expectedBlockHash;

        public InsertTransactionEngineState(
            string logId,
            string blockHash)
        {
            _expectedGuid = Guid.Parse(logId);
            _expectedBlockHash = UInt256.Parse(blockHash);
            _snapshot = TestStorage.Store.GetSnapshot();
        }

        public override void Before(MethodInfo methodUnderTest)
        {
            using var lss = new LogStorageStore(_snapshot);
            lss.PutTransactionEngineState(_expectedBlockHash, TransactionEngineLogState.Create([_expectedGuid]));
            _snapshot.Commit();
        }

        public override void After(MethodInfo methodUnderTest)
        {
            _snapshot.Dispose();
        }
    }
}
