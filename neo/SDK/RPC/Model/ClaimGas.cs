using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class ClaimGas
    {
        [JsonProperty(PropertyName = "txid")]
        public string TxId { get; set; }

        [JsonProperty(PropertyName = "size")]
        public int Size { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "attributes")]
        public object[] Attributes { get; set; }

        [JsonProperty(PropertyName = "vin")]
        public object[] Vin { get; set; }

        [JsonProperty(PropertyName = "vout")]
        public string[] Vout { get; set; }

        [JsonProperty(PropertyName = "sys_fee")]
        public string SysFee { get; set; }

        [JsonProperty(PropertyName = "net_fee")]
        public string NetFee { get; set; }

        [JsonProperty(PropertyName = "scripts")]
        public Script[] Scripts { get; set; }

        [JsonProperty(PropertyName = "claims")]
        public Claim[] Claims { get; set; }

    }

    public class Script
    {
        [JsonProperty(PropertyName = "invocation")]
        public string Invocation { get; set; }

        [JsonProperty(PropertyName = "verification")]
        public string Verification { get; set; }
    }

    public class Claim
    {
        [JsonProperty(PropertyName = "txid")]
        public string TxId { get; set; }

        [JsonProperty(PropertyName = "vout")]
        public int Vout { get; set; }

    }

}
