using Neo.IO.Json;
using System.Linq;

namespace Neo.Network.RPC.Models
{
    public class RpcPeers
    {
        public RpcPeer[] Unconnected { get; set; }

        public RpcPeer[] Bad { get; set; }

        public RpcPeer[] Connected { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["unconnected"] = new JArray(Unconnected.Select(p => p.ToJson()));
            json["bad"] = new JArray(Bad.Select(p => p.ToJson()));
            json["connected"] = new JArray(Connected.Select(p => p.ToJson()));
            return json;
        }

        public static RpcPeers FromJson(JObject json)
        {
            RpcPeers result = new RpcPeers();
            result.Unconnected = ((JArray)json["unconnected"]).Select(p => RpcPeer.FromJson(p)).ToArray();
            result.Bad = ((JArray)json["bad"]).Select(p => RpcPeer.FromJson(p)).ToArray();
            result.Connected = ((JArray)json["connected"]).Select(p => RpcPeer.FromJson(p)).ToArray();
            return result;
        }
    }

    public class RpcPeer
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

        public static RpcPeer FromJson(JObject json)
        {
            RpcPeer peer = new RpcPeer();
            peer.Address = json["address"].AsString();
            peer.Port = int.Parse(json["port"].AsString());
            return peer;
        }
    }
}
