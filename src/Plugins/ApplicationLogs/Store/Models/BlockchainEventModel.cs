// Copyright (C) 2015-2024 The Neo Project.
//
// BlockchainEventModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.ApplicationLogs.Store.States;
using Neo.VM.Types;

namespace Neo.Plugins.ApplicationLogs.Store.Models
{
    public class BlockchainEventModel
    {
        public required UInt160 ScriptHash { get; init; }
        public required string EventName { get; init; }
        public required StackItem[] State { get; init; }

        public static BlockchainEventModel Create(UInt160 scriptHash, string eventName, params StackItem[] state) =>
            new()
            {
                ScriptHash = scriptHash,
                EventName = eventName ?? string.Empty,
                State = state,
            };

        public static BlockchainEventModel Create(NotifyLogState notifyLogState, params StackItem[] state) =>
            new()
            {
                ScriptHash = notifyLogState.ScriptHash,
                EventName = notifyLogState.EventName,
                State = state,
            };

        public static BlockchainEventModel Create(ContractLogState contractLogState, params StackItem[] state) =>
            new()
            {
                ScriptHash = contractLogState.ScriptHash,
                EventName = contractLogState.EventName,
                State = state,
            };
    }
}
