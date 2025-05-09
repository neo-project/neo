// Copyright (C) 2015-2025 The Neo Project.
//
// DebugApplicationEngine.cs file belongs to the neo project and is free
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
using System.Collections.Generic;

namespace Neo.Build.Core.SmartContract
{
    internal class DebugApplicationEngine : ApplicationEngineBase
    {
        public DebugApplicationEngine(
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            long maxGas,
            StorageSettings? storageSettings = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            ILoggerFactory? loggerFactory = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : base(
                new ApplicationEngineSettings(),
                protocolSettings,
                snapshotCache,
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
