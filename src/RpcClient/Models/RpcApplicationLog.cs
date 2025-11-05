// Copyright (C) 2015-2025 The Neo Project.
//
// RpcApplicationLog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Json;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcApplicationLog
    {
        public UInt256 TxId { get; set; }

        public UInt256 BlockHash { get; set; }

        public List<Execution> Executions { get; set; }

        public JsonObject ToJson()
        {
            var json = new JsonObject();
            if (TxId != null)
                json["txid"] = TxId.ToString();
            if (BlockHash != null)
                json["blockhash"] = BlockHash.ToString();
            json["executions"] = new JsonArray(Executions.Select(p => p.ToJson()).ToArray());
            return json;
        }

        public static RpcApplicationLog FromJson(JsonObject json, ProtocolSettings protocolSettings)
        {
            return new RpcApplicationLog
            {
                TxId = json["txid"] is null ? null : UInt256.Parse(json["txid"].AsString()),
                BlockHash = json["blockhash"] is null ? null : UInt256.Parse(json["blockhash"].AsString()),
                Executions = ((JsonArray)json["executions"]).Select(p => Execution.FromJson((JsonObject)p, protocolSettings)).ToList(),
            };
        }
    }

    public class Execution
    {
        public TriggerType Trigger { get; set; }

        public VMState VMState { get; set; }

        public long GasConsumed { get; set; }

        public string ExceptionMessage { get; set; }

        public List<StackItem> Stack { get; set; }

        public List<RpcNotifyEventArgs> Notifications { get; set; }

        public JsonObject ToJson()
        {
            return new()
            {
                ["trigger"] = Trigger.ToString(),
                ["vmstate"] = VMState.ToString(),
                ["gasconsumed"] = GasConsumed.ToString(),
                ["exception"] = ExceptionMessage,
                ["stack"] = new JsonArray(Stack.Select(q => q.ToJson()).ToArray()),
                ["notifications"] = new JsonArray(Notifications.Select(q => q.ToJson()).ToArray())
            };
        }

        public static Execution FromJson(JsonObject json, ProtocolSettings protocolSettings)
        {
            return new Execution
            {
                Trigger = json["trigger"].GetEnum<TriggerType>(),
                VMState = json["vmstate"].GetEnum<VMState>(),
                GasConsumed = long.Parse(json["gasconsumed"].AsString()),
                ExceptionMessage = json["exception"]?.AsString(),
                Stack = ((JsonArray)json["stack"]).Select(p => Utility.StackItemFromJson((JsonObject)p)).ToList(),
                Notifications = ((JsonArray)json["notifications"]).Select(p => RpcNotifyEventArgs.FromJson((JsonObject)p, protocolSettings)).ToList()
            };
        }
    }

    public class RpcNotifyEventArgs
    {
        public UInt160 Contract { get; set; }

        public string EventName { get; set; }

        public StackItem State { get; set; }

        public JsonObject ToJson()
        {
            return new()
            {
                ["contract"] = Contract.ToString(),
                ["eventname"] = EventName,
                ["state"] = State.ToJson(),
            };
        }

        public static RpcNotifyEventArgs FromJson(JsonObject json, ProtocolSettings protocolSettings)
        {
            return new RpcNotifyEventArgs
            {
                Contract = json["contract"].ToScriptHash(protocolSettings),
                EventName = json["eventname"].AsString(),
                State = Utility.StackItemFromJson((JsonObject)json["state"])
            };
        }
    }
}
