using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_Version
    {
        [JsonProperty(PropertyName = "tcpPort")]
        public int TcpPort { get; set; }

        [JsonProperty(PropertyName = "wsPort")]
        public int WsPort { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public uint Nonce { get; set; }

        [JsonProperty(PropertyName = "useragent")]
        public string UserAgent { get; set; }

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
