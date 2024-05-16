// Copyright (C) 2015-2024 The Neo Project.
//
// RpcApplicationLog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.RPC.Models
{
    public class RpcApplicationLog
    {
        public UInt256 TxId { get; set; }

        public UInt256 BlockHash { get; set; }

        public List<Execution> Executions { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            if (TxId != null)
                json["txid"] = TxId.ToString();
            if (BlockHash != null)
                json["blockhash"] = BlockHash.ToString();
            json["executions"] = Executions.Select(p => p.ToJson()).ToArray();
            return json;
        }

        public static RpcApplicationLog FromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new RpcApplicationLog
            {
                TxId = json["txid"] is null ? null : UInt256.Parse(json["txid"].AsString()),
                BlockHash = json["blockhash"] is null ? null : UInt256.Parse(json["blockhash"].AsString()),
                Executions = ((JArray)json["executions"]).Select(p => Execution.FromJson((JObject)p, protocolSettings)).ToList(),
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

        public JObject ToJson()
        {
            JObject json = new();
            json["trigger"] = Trigger;
            json["vmstate"] = VMState;
            json["gasconsumed"] = GasConsumed.ToString();
            json["exception"] = ExceptionMessage;
            json["stack"] = Stack.Select(q => q.ToJson()).ToArray();
            json["notifications"] = Notifications.Select(q => q.ToJson()).ToArray();
            return json;
        }

        public static Execution FromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new Execution
            {
                Trigger = json["trigger"].GetEnum<TriggerType>(),
                VMState = json["vmstate"].GetEnum<VMState>(),
                GasConsumed = long.Parse(json["gasconsumed"].AsString()),
                ExceptionMessage = json["exception"]?.AsString(),
                Stack = ((JArray)json["stack"]).Select(p => Utility.StackItemFromJson((JObject)p)).ToList(),
                Notifications = ((JArray)json["notifications"]).Select(p => RpcNotifyEventArgs.FromJson((JObject)p, protocolSettings)).ToList()
            };
        }
    }

    public class RpcNotifyEventArgs
    {
        public UInt160 Contract { get; set; }

        public string EventName { get; set; }

        public StackItem State { get; set; }

        public JObject ToJson()
        {
            JObject json = new();
            json["contract"] = Contract.ToString();
            json["eventname"] = EventName;
            json["state"] = State.ToJson();
            return json;
        }

        public static RpcNotifyEventArgs FromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new RpcNotifyEventArgs
            {
                Contract = json["contract"].ToScriptHash(protocolSettings),
                EventName = json["eventname"].AsString(),
                State = Utility.StackItemFromJson((JObject)json["state"])
            };
        }
    }
}
