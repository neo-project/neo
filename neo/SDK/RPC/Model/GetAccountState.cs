using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetAccountState
    {
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "script_hash")]
        public string ScriptHash { get; set; }

        [JsonProperty(PropertyName = "frozen")]
        public string Frozen { get; set; }

        [JsonProperty(PropertyName = "votes")]
        public string[] Votes { get; set; }

        [JsonProperty(PropertyName = "balances")]
        public StateBalance[] Balances { get; set; }

    }

    public class StateBalance
    {
        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

    }
}
