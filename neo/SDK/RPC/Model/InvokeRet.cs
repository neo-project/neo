using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class InvokeRet
    {
        [JsonProperty(PropertyName = "script")]
        public string Script { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "gas_consumed")]
        public string GasConsumed { get; set; }

        [JsonProperty(PropertyName = "stack")]
        public Stack[] Stack { get; set; }

        [JsonProperty(PropertyName = "tx")]
        public string Tx { get; set; }
    }

    public class Stack
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }

}
