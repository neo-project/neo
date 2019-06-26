using Neo.IO.Json;
using System.Linq;

namespace Neo.SDK.RPC.Model
{
    public class SDK_GetPeersResult
    {
        public SDK_Peer[] Unconnected { get; set; }
        
        public SDK_Peer[] Bad { get; set; }
        
        public SDK_Peer[] Connected { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["unconnected"] = new JArray(Unconnected.Select(p => p.ToJson()));
            json["bad"] = new JArray(Bad.Select(p => p.ToJson()));
            json["connected"] = new JArray(Connected.Select(p => p.ToJson()));
            return json;
        }

        public static SDK_GetPeersResult FromJson(JObject json)
        {
            SDK_GetPeersResult result = new SDK_GetPeersResult();
            result.Unconnected = ((JArray)json["unconnected"]).Select(p => SDK_Peer.FromJson(p)).ToArray();
            result.Bad = ((JArray)json["bad"]).Select(p => SDK_Peer.FromJson(p)).ToArray();
            result.Connected = ((JArray)json["connected"]).Select(p => SDK_Peer.FromJson(p)).ToArray();
            return result;
        }
    }

    public class SDK_Peer
    {
        public string Address { get; set; }
        
        public int Port { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["address"] = Address;
            json["port"] = Port;
            return json;
        }

        public static SDK_Peer FromJson(JObject json)
        {
            SDK_Peer peer = new SDK_Peer();
            peer.Address = json["address"].AsString();
            peer.Port = int.Parse(json["port"].AsString());
            return peer;
        }
    }
}
