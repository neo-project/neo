// Copyright (C) 2015-2024 The Neo Project.
//
// BlockchainExecutionModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using ApplicationLogs.Store.States;
using Neo.Plugins.Store.Models;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace ApplicationLogs.Store.Models
{
    public class BlockchainExecutionModel
    {
        public TriggerType Trigger { get; private init; } = TriggerType.All;
        public VMState VmState { get; private init; } = VMState.NONE;
        public string Exception { get; private init; } = string.Empty;
        public long GasConsumed { get; private init; } = 0L;
        public StackItem[] Stack { get; private init; } = [];
        public BlockchainEventModel[] Notifications { get; set; } = [];
        public ApplicationEngineLogModel[] Logs { get; set; } = [];

        public static BlockchainExecutionModel Create(TriggerType trigger, ExecutionLogState executionLogState, StackItem[] stack) =>
            new()
            {
                Trigger = trigger,
                VmState = executionLogState.VmState,
                Exception = executionLogState.Exception ?? string.Empty,
                GasConsumed = executionLogState.GasConsumed,
                Stack = stack,
            };
    }
}
