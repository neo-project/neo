using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_BlockHeader
    {
        public Header Header { get; set; }

        [JsonProperty(PropertyName = "confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty(PropertyName = "nextblockhash")]
        public UInt256 NextBlockHash { get; set; }

        public JObject ToJson()
        {
            JObject json = Header.ToJson();
            json["confirmations"] = Confirmations;
            json["nextblockhash"] = NextBlockHash.ToString();
            return json;
        }

        public static SDK_BlockHeader FromJson(JObject json)
        {
            SDK_BlockHeader block = new SDK_BlockHeader();
            block.Confirmations = (int)json["confirmations"].AsNumber();
            block.NextBlockHash = UInt256.Parse(json["nextblockhash"].AsString());
            block.Header = Header.FromJson(json);
            return block;
        }
    }
}
