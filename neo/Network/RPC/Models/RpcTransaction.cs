using Neo.IO.Json;
using Neo.Network.P2P.Payloads;

namespace Neo.Network.RPC.Models
{
    public class RpcTransaction
    {
        public Transaction Transaction { get; set; }

        public UInt256 BlockHash { get; set; }

        public int? Confirmations { get; set; }

        public uint? BlockTime { get; set; }

        public JObject ToJson()
        {
            JObject json = Transaction.ToJson();
            if (Confirmations != null)
            {
                json["blockhash"] = BlockHash.ToString();
                json["confirmations"] = Confirmations;
                json["blocktime"] = BlockTime;
            }
            return json;
        }

        public static RpcTransaction FromJson(JObject json)
        {
            RpcTransaction transaction = new RpcTransaction();
            transaction.Transaction = Transaction.FromJson(json);
            if (json["confirmations"] != null)
            {
                transaction.BlockHash = UInt256.Parse(json["blockhash"].AsString());
                transaction.Confirmations = (int)json["confirmations"].AsNumber();
                transaction.BlockTime = (uint)json["blocktime"].AsNumber();
            }
            return transaction;
        }
    }
}
