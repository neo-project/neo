// Copyright (C) 2015-2025 The Neo Project.
//
// BlockchainExecutionModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.ApplicationLogs.Store.States;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Plugins.ApplicationLogs.Store.Models
{
    public class BlockchainExecutionModel
    {
        public required TriggerType Trigger { get; init; }
        public required VMState VmState { get; init; }
        public required string Exception { get; init; }
        public required long GasConsumed { get; init; }
        public required StackItem[] Stack { get; init; }
        public required BlockchainEventModel[] Notifications { get; set; }
        public required ApplicationEngineLogModel[] Logs { get; set; }

        public static BlockchainExecutionModel Create(TriggerType trigger, ExecutionLogState executionLogState, params StackItem[] stack) =>
            new()
            {
                Trigger = trigger,
                VmState = executionLogState.VmState,
                Exception = executionLogState.Exception ?? string.Empty,
                GasConsumed = executionLogState.GasConsumed,
                Stack = stack,
                Notifications = [],
                Logs = []
            };
    }
}
