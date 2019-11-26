using Neo.IO.Json;

namespace Neo.Network.RPC.Models
{
    public class RpcVersion
    {
        public int TcpPort { get; set; }

        public int WsPort { get; set; }

        public uint Nonce { get; set; }

        public string UserAgent { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["topPort"] = TcpPort.ToString();
            json["wsPort"] = WsPort.ToString();
            json["nonce"] = Nonce.ToString();
            json["useragent"] = UserAgent;
            return json;
        }

        public static RpcVersion FromJson(JObject json)
        {
            RpcVersion version = new RpcVersion();
            version.TcpPort = int.Parse(json["tcpPort"].AsString());
            version.WsPort = int.Parse(json["wsPort"].AsString());
            version.Nonce = uint.Parse(json["nonce"].AsString());
            version.UserAgent = json["useragent"].AsString();
            return version;
        }
    }
}
