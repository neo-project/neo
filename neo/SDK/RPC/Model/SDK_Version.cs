using Neo.IO.Json;

namespace Neo.SDK.RPC.Model
{
    public class SDK_Version
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

        public static SDK_Version FromJson(JObject json)
        {
            SDK_Version version = new SDK_Version();
            version.TcpPort = int.Parse(json["tcpPort"].AsString());
            version.WsPort = int.Parse(json["wsPort"].AsString());
            version.Nonce = uint.Parse(json["nonce"].AsString());
            version.UserAgent = json["useragent"].AsString();
            return version;
        }
    }
}
