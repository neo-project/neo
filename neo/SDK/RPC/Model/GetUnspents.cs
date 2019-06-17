using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetUnspents
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "balance")]
        public UtxoBalance[] Balances { get; set; }
    }

    public class UtxoBalance
    {
        [JsonProperty(PropertyName = "asset_hash")]
        public string AssetHash { get; set; }

        [JsonProperty(PropertyName = "asset")]
        public string Asset { get; set; }

        [JsonProperty(PropertyName = "asset_symbol")]
        public string AssetSymbol { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; set; }

        [JsonProperty(PropertyName = "unspent")]
        public Unspent[] Unspents { get; set; }

    }

    public class Unspent
    {
        [JsonProperty(PropertyName = "txid")]
        public string TxId { get; set; }

        [JsonProperty(PropertyName = "n")]
        public int N { get; set; }

        [JsonProperty(PropertyName = "value")]
        public decimal Value { get; set; }
    }


}
