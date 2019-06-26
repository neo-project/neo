using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_GetPeersResult
    {
        [JsonProperty(PropertyName = "unconnected")]
        public SDK_Peer[] Unconnected { get; set; }

        [JsonProperty(PropertyName = "bad")]
        public SDK_Peer[] Bad { get; set; }

        [JsonProperty(PropertyName = "connected")]
        public SDK_Peer[] Connected { get; set; }

        public static SDK_GetPeersResult FromJson(JObject json)
        {
            SDK_GetPeersResult result = new SDK_GetPeersResult();
            result.Unconnected = ((JArray)json["unconnected"]).Select(p => SDK_Peer.FromJson(p)).ToArray();
            result.Unconnected = ((JArray)json["bad"]).Select(p => SDK_Peer.FromJson(p)).ToArray();
            result.Unconnected = ((JArray)json["connected"]).Select(p => SDK_Peer.FromJson(p)).ToArray();
            return result;
        }
    }

    public class SDK_Peer
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }

        public static SDK_Peer FromJson(JObject json)
        {
            SDK_Peer peer = new SDK_Peer();
            peer.Address = json["address"].AsString();
            peer.Port = int.Parse(json["port"].AsString());
            return peer;
        }
    }
}
