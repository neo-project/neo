// Copyright (C) 2015-2025 The Neo Project.
//
// TestApplicationEngineProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.Core.SmartContract;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.Build.Core.Providers
{
    public class TestApplicationEngineProvider : IApplicationEngineProvider
    {
        private readonly StorageSettings _storageSettings;
        private readonly ILoggerFactory _loggerFactory;

        public TestApplicationEngineProvider(
            StorageSettings storageSettings,
            ILoggerFactory loggerFactory)
        {
            _storageSettings = storageSettings;
            _loggerFactory = loggerFactory;
        }

        public ApplicationEngine Create(
            TriggerType trigger,
            IVerifiable container,
            DataCache snapshot,
            Block persistingBlock,
            ProtocolSettings settings,
            long gas,
            IDiagnostic diagnostic,
            JumpTable jumpTable) =>
            new TestApplicationEngine(
                settings,
                snapshot,
                gas,
                _storageSettings,
                _loggerFactory,
                trigger,
                container,
                persistingBlock,
                diagnostic);
    }
}
