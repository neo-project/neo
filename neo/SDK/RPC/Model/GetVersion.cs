using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetVersion
    {
        [JsonProperty(PropertyName = "tcpPort")]
        public int TcpPort { get; set; }

        [JsonProperty(PropertyName = "wsPort")]
        public int WsPort { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public uint Nonce { get; set; }

        [JsonProperty(PropertyName = "useragent")]
        public string UserAgent { get; set; }
    }

}
