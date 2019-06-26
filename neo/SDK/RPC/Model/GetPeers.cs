using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetPeers
    {
        [JsonProperty(PropertyName = "unconnected")]
        public Peer[] Unconnected { get; set; }

        [JsonProperty(PropertyName = "bad")]
        public Peer[] Bad { get; set; }

        [JsonProperty(PropertyName = "connected")]
        public Peer[] Connected { get; set; }
    }

    public class Peer
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
    }
}
