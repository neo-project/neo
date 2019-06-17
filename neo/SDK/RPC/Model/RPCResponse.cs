using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    /// <summary>
    /// RPC Response from rpc server
    /// </summary>
    /// <typeparam name="T">Specific type response</typeparam>
    public class RPCResponse<T>
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "jsonrpc")]
        public string Jsonrpc { get; set; }

        [JsonProperty(PropertyName = "error")]
        public RPCResponseError Error { get; set; }

        [JsonProperty(PropertyName = "result")]
        public T Result { get; set; }
    }

    public class RPCResponseError
    {
        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "data")]
        public object Data { get; set; }
    }

}
