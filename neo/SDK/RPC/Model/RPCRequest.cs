using Neo.IO.Json;
using Newtonsoft.Json;
using System.Linq;

namespace Neo.SDK.RPC.Model
{
    public class RPCRequest
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "jsonrpc")]
        public string Jsonrpc { get; set; }

        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "params")]
        public JObject[] Params { get; set; }

        /// <summary>
        /// Parse from json
        /// </summary>
        public static RPCRequest FromJson(JObject json)
        {
            return new RPCRequest
            {
                Id = (int)json["id"].AsNumber(),
                Jsonrpc = json["jsonrpc"].AsString(),
                Method = json["method"].AsString(),
                Params = ((JArray)json["params"]).ToArray()
            };
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["id"] = Id;
            json["jsonrpc"] = Jsonrpc;
            json["method"] = Method;
            json["params"] = new JArray(Params);
            return json;
        }

    }

}
