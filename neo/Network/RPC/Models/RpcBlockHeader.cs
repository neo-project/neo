using Neo.IO.Json;
using Neo.Network.P2P.Payloads;

namespace Neo.Network.RPC.Models
{
    public class RpcBlockHeader
    {
        public Header Header { get; set; }

        public int? Confirmations { get; set; }

        public UInt256 NextBlockHash { get; set; }

        public JObject ToJson()
        {
            JObject json = Header.ToJson();
            if (Confirmations != null)
            {
                json["confirmations"] = Confirmations;
                json["nextblockhash"] = NextBlockHash.ToString();
            }
            return json;
        }

        public static RpcBlockHeader FromJson(JObject json)
        {
            RpcBlockHeader block = new RpcBlockHeader();
            block.Header = Header.FromJson(json);
            if (json["confirmations"] != null)
            {
                block.Confirmations = (int)json["confirmations"].AsNumber();
                block.NextBlockHash = UInt256.Parse(json["nextblockhash"].AsString());
            }
            return block;
        }
    }
}
