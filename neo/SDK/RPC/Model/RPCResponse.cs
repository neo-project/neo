using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    /// <summary>
    /// RPC Response from rpc server
    /// </summary>
    public class RPCResponse
    {
        [JsonProperty(PropertyName = "id")]
        public int? Id { get; set; }

        [JsonProperty(PropertyName = "jsonrpc")]
        public string Jsonrpc { get; set; }

        [JsonProperty(PropertyName = "error")]
        public RPCResponseError Error { get; set; }

        [JsonProperty(PropertyName = "result")]
        public JObject Result { get; set; }

        /// <summary>
        /// Parse from json
        /// </summary>
        public static RPCResponse FromJson(JObject json)
        {
            var response = new RPCResponse
            {
                Id = (int?)json["id"]?.AsNumber(),
                Jsonrpc = json["jsonrpc"].AsString(),
                Result = json["result"]
            };

            if (json["error"] != null)
            {
                response.Error = RPCResponseError.FromJson(json["error"]);
            }

            return response;
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["id"] = Id;
            json["jsonrpc"] = Jsonrpc;
            json["error"] = Error.ToJson();
            json["result"] = Result;
            return json;
        }
    }

    public class RPCResponseError
    {
        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "data")]
        public JObject Data { get; set; }

        /// <summary>
        /// Parse from json
        /// </summary>
        public static RPCResponseError FromJson(JObject json)
        {
            return new RPCResponseError
            {
                Code = (int)json["code"].AsNumber(),
                Message = json["message"].AsString(),
                Data = json["data"],
            };
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["code"] = Code;
            json["message"] = Message;
            json["data"] = Data;
            return json;
        }
    }


}
