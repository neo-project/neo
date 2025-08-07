// Copyright (C) 2015-2025 The Neo Project.
//
// UnitTestApplicationEngineProvider.cs file belongs to the neo project and is free
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
using Neo.VM;

namespace Neo.Build.Core.Tests.Helpers.SmartContract
{
    internal class UnitTestApplicationEngineProvider : IApplicationEngineProvider
    {
        private readonly ILoggerFactory FactoryLogger = LoggerFactory.Create(logging =>
        {
            logging.AddDebug();
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        });

        public static UnitTestApplicationEngineProvider Instance => new();

        public ApplicationEngine Create(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, ProtocolSettings settings, long gas, IDiagnostic diagnostic, JumpTable jumpTable) =>
            new UnitTestApplicationEngine(settings, snapshot, gas, new(), trigger, container, persistingBlock, diagnostic, FactoryLogger);
    }
}
