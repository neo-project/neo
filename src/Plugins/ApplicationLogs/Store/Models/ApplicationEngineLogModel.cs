// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineLogModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.ApplicationLogs.Store.States;

namespace Neo.Plugins.ApplicationLogs.Store.Models
{
    public class ApplicationEngineLogModel
    {
        public required UInt160 ScriptHash { get; init; }
        public required string Message { get; init; }

        public static ApplicationEngineLogModel Create(EngineLogState logEventState) =>
            new()
            {
                ScriptHash = logEventState.ScriptHash,
                Message = logEventState.Message,
            };
    }
}
