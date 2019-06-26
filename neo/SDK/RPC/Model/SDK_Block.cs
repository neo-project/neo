using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_Block
    {
        public Block Block { get; set; }

        [JsonProperty(PropertyName = "confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty(PropertyName = "nextblockhash")]
        public UInt256 NextBlockHash { get; set; }

        public JObject ToJson()
        {
            JObject json = Block.ToJson();
            json["confirmations"] = Confirmations;
            json["nextblockhash"] = NextBlockHash.ToString();
            return json;
        }

        public static SDK_Block FromJson(JObject json)
        {
            SDK_Block block = new SDK_Block();
            block.Confirmations = (int)json["confirmations"].AsNumber();
            block.NextBlockHash = UInt256.Parse(json["nextblockhash"].AsString());
            block.Block = Block.FromJson(json);
            return block;
        }
    }


}
