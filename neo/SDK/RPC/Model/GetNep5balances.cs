using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetNep5Balances
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "balance")]
        public Nep5Balance[] Balances { get; set; }
    }

    public class Nep5Balance
    {
        [JsonProperty(PropertyName = "asset_hash")]
        public string AssetHash { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public string Amount { get; set; }

        [JsonProperty(PropertyName = "last_updated_block")]
        public int last_updated_block { get; set; }
    }


}
