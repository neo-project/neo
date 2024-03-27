// Copyright (C) 2015-2024 The Neo Project.
//
// TraceApplicationEngine.Internals.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.SmartContract;

namespace Neo.Service.Engines
{
    internal partial class TraceApplicationEngine
    {
        internal static string GenerateLoggerCategoryName(ProtocolSettings? protocolSettings = null, Block? persistingBlock = null, IVerifiable? container = null)
        {
            var logCategory = nameof(ApplicationEngine);

            if (protocolSettings is not null)
                logCategory += $".Networks.{protocolSettings.Network}";
            if (persistingBlock is not null)
                logCategory += $".Blocks.{persistingBlock.Index}";
            if (container is not null and Transaction tx)
                logCategory += $".Transactions.{container.Hash}";

            return logCategory;
        }
    }
}
