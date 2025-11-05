// Copyright (C) 2015-2025 The Neo Project.
//
// RpcInvokeResult.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcInvokeResult
    {
        public string Script { get; set; }

        public VMState State { get; set; }

        public long GasConsumed { get; set; }

        public StackItem[] Stack { get; set; }

        public string Tx { get; set; }

        public string Exception { get; set; }

        public string Session { get; set; }

        public JsonObject ToJson()
        {
            var json = new JsonObject()
            {
                ["script"] = Script,
                ["state"] = State.ToString(),
                ["gasconsumed"] = GasConsumed.ToString()
            };

            if (!string.IsNullOrEmpty(Exception))
                json["exception"] = Exception;

            try
            {
                json["stack"] = new JsonArray(Stack.Select(p => p.ToJson()).ToArray());
            }
            catch (InvalidOperationException)
            {
                // ContractParameter.ToJson() may cause InvalidOperationException
                json["stack"] = "error: recursive reference";
            }

            if (!string.IsNullOrEmpty(Tx)) json["tx"] = Tx;
            return json;
        }

        public static RpcInvokeResult FromJson(JsonObject json)
        {
            var invokeScriptResult = new RpcInvokeResult()
            {
                Script = json["script"].AsString(),
                State = json["state"].GetEnum<VMState>(),
                GasConsumed = long.Parse(json["gasconsumed"].AsString()),
                Exception = json["exception"]?.AsString(),
                Session = json["session"]?.AsString()
            };
            try
            {
                invokeScriptResult.Stack = ((JsonArray)json["stack"]).Select(p => Utility.StackItemFromJson((JsonObject)p)).ToArray();
            }
            catch { }
            invokeScriptResult.Tx = json["tx"]?.AsString();
            return invokeScriptResult;
        }
    }

    public class RpcStack
    {
        public string Type { get; set; }

        public JsonNode Value { get; set; }

        public JsonObject ToJson() => new() { ["type"] = Type, ["value"] = Value };

        public static RpcStack FromJson(JsonObject json)
        {
            return new RpcStack
            {
                Type = json["type"].AsString(),
                Value = json["value"]
            };
        }
    }
}
