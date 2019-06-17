using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetClaimable
    {
        [JsonProperty(PropertyName = "claimable")]
        public Claimable[] Claimables { get; set; }

        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "unclaimed")]
        public decimal Unclaimed { get; set; }
    }

    public class Claimable
    {
        [JsonProperty(PropertyName = "txid")]
        public string TxId { get; set; }

        [JsonProperty(PropertyName = "n")]
        public int N { get; set; }

        [JsonProperty(PropertyName = "value")]
        public decimal Value { get; set; }

        [JsonProperty(PropertyName = "start_height")]
        public int StartHeight { get; set; }

        [JsonProperty(PropertyName = "end_height")]
        public int EndHeight { get; set; }

        [JsonProperty(PropertyName = "generated")]
        public decimal Generated { get; set; }

        [JsonProperty(PropertyName = "sys_fee")]
        public decimal SysFee { get; set; }

        [JsonProperty(PropertyName = "unclaimed")]
        public decimal Unclaimed { get; set; }
    }
}