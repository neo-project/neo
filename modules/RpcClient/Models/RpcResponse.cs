// Copyright (C) 2015-2024 The Neo Project.
//
// RpcResponse.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;

namespace Neo.Network.RPC.Models
{
    public class RpcResponse
    {
        public JToken Id { get; set; }

        public string JsonRpc { get; set; }

        public RpcResponseError Error { get; set; }

        public JToken Result { get; set; }

        public string RawResponse { get; set; }

        public static RpcResponse FromJson(JObject json)
        {
            RpcResponse response = new()
            {
                Id = json["id"],
                JsonRpc = json["jsonrpc"].AsString(),
                Result = json["result"]
            };

            if (json["error"] != null)
            {
                response.Error = RpcResponseError.FromJson((JObject)json["error"]);
            }

            return response;
        }

        public JObject ToJson()
        {
            JObject json = new();
            json["id"] = Id;
            json["jsonrpc"] = JsonRpc;
            json["error"] = Error?.ToJson();
            json["result"] = Result;
            return json;
        }
    }

    public class RpcResponseError
    {
        public int Code { get; set; }

        public string Message { get; set; }

        public JToken Data { get; set; }

        public static RpcResponseError FromJson(JObject json)
        {
            return new RpcResponseError
            {
                Code = (int)json["code"].AsNumber(),
                Message = json["message"].AsString(),
                Data = json["data"],
            };
        }

        public JObject ToJson()
        {
            JObject json = new();
            json["code"] = Code;
            json["message"] = Message;
            json["data"] = Data;
            return json;
        }
    }
}
