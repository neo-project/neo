using Neo.IO.Json;

namespace Neo.SDK.RPC.Model
{
    /// <summary>
    /// RPC Response from rpc server
    /// </summary>
    public class RPCResponse
    {
        public int? Id { get; set; }

        public string Jsonrpc { get; set; }

        public RPCResponseError Error { get; set; }

        public JObject Result { get; set; }

        public string RawResponse { get; set; }

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
        public int Code { get; set; }

        public string Message { get; set; }

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
