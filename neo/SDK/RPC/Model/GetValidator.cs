using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class Validator
    {
        [JsonProperty(PropertyName = "publickey")]
        public string PublicKey { get; set; }

        [JsonProperty(PropertyName = "votes")]
        public string Votes { get; set; }

        [JsonProperty(PropertyName = "active")]
        public bool Active { get; set; }
    }

}
