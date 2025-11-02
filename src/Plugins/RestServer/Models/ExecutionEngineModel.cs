// Copyright (C) 2015-2025 The Neo Project.
//
// ExecutionEngineModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.RestServer.Models.Error;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Plugins.RestServer.Models
{
    internal class ExecutionEngineModel
    {
        public long GasConsumed { get; set; } = 0L;
        public VMState State { get; set; } = VMState.NONE;
        public BlockchainEventModel[] Notifications { get; set; } = System.Array.Empty<BlockchainEventModel>();
        public StackItem[] ResultStack { get; set; } = System.Array.Empty<StackItem>();
        public ErrorModel? FaultException { get; set; }
    }

    internal class BlockchainEventModel
    {
        public UInt160 ScriptHash { get; set; } = new();
        public string EventName { get; set; } = string.Empty;
        public StackItem[] State { get; set; } = System.Array.Empty<StackItem>();

        public static BlockchainEventModel Create(UInt160 scriptHash, string eventName, StackItem[] state) =>
            new()
            {
                ScriptHash = scriptHash,
                EventName = eventName ?? string.Empty,
                State = state,
            };

        public static BlockchainEventModel Create(NotifyEventArgs notifyEventArgs, StackItem[] state) =>
            new()
            {
                ScriptHash = notifyEventArgs.ScriptHash,
                EventName = notifyEventArgs.EventName,
                State = state,
            };
    }
}
