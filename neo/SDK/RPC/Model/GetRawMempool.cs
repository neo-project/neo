using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetRawMempool
    {
        [JsonProperty(PropertyName = "height")]
        public uint Height { get; set; }

        [JsonProperty(PropertyName = "verified")]
        public string[] Verified { get; set; }

        [JsonProperty(PropertyName = "unverified")]
        public string[] UnVerified { get; set; }
    }

}
