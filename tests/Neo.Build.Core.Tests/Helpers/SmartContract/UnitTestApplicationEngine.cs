// Copyright (C) 2015-2025 The Neo Project.
//
// UnitTestApplicationEngine.cs file belongs to the neo project and is free
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
using System.Collections.Generic;

namespace Neo.Build.Core.Tests.Helpers.SmartContract
{
    internal class UnitTestApplicationEngine : ApplicationEngineBase
    {
        public UnitTestApplicationEngine(
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            long maxGas = 20_0000000L,
            StorageSettings? storageSettings = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            ILoggerFactory? loggerFactory = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : base(protocolSettings,
                  snapshotCache,
                  maxGas,
                  storageSettings,
                  trigger,
                  container,
                  persistingBlock,
                  diagnostic,
                  loggerFactory,
                  systemCallMethods)
        {
        }
    }
}
