// Copyright (C) 2015-2025 The Neo Project.
//
// TestApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;

namespace Neo.Build.Core.SmartContract
{
    public class TestApplicationEngine : ApplicationEngineBase
    {
        public TestApplicationEngine(
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            long maxGas,
            StorageSettings? storageSettings = null,
            ILoggerFactory? loggerFactory = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null)
            : base(
                  protocolSettings,
                  snapshotCache,
                  maxGas,
                  storageSettings,
                  loggerFactory,
                  trigger,
                  container,
                  persistingBlock,
                  diagnostic,
                  null)
        { }

        public TestApplicationEngine(
            NeoBuildSettings settings,
            DataCache snapshot,
            ILoggerFactory loggerFactory)
            : base(
                  settings,
                  snapshot,
                  loggerFactory)
        {

        }
    }
}
