using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class GetBlock
    {
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "size")]
        public uint Size { get; set; }

        [JsonProperty(PropertyName = "version")]
        public uint Version { get; set; }

        [JsonProperty(PropertyName = "previousblockhash")]
        public string PreviousBlockHash { get; set; }

        [JsonProperty(PropertyName = "merkleroot")]
        public string MerkleRoot { get; set; }

        [JsonProperty(PropertyName = "time")]
        public uint Time { get; set; }

        [JsonProperty(PropertyName = "index")]
        public uint Index { get; set; }

        [JsonProperty(PropertyName = "nextconsensus")]
        public string NextConsensus { get; set; }

        [JsonProperty(PropertyName = "witness")]
        public WitnessJson Witness { get; set; }

        [JsonProperty(PropertyName = "consensus_data")]
        public ConsensusData ConsensusData { get; set; }

        [JsonProperty(PropertyName = "tx")]
        public TxJson[] Tx { get; set; }

        [JsonProperty(PropertyName = "confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty(PropertyName = "nextblockhash")]
        public string NextBlockHash { get; set; }

    }

    public class WitnessJson
    {
        [JsonProperty(PropertyName = "invocation")]
        public string Invocation { get; set; }

        [JsonProperty(PropertyName = "verification")]
        public string Verification { get; set; }
    }

    public class ConsensusData
    {
        [JsonProperty(PropertyName = "primary")]
        public string Primary { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }
    }

    public class TxJson
    {
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "size")]
        public uint Size { get; set; }

        [JsonProperty(PropertyName = "version")]
        public uint Version { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public uint Nonce { get; set; }

        [JsonProperty(PropertyName = "script")]
        public string Script { get; set; }

        [JsonProperty(PropertyName = "sender")]
        public string Sender { get; set; }

        [JsonProperty(PropertyName = "gas")]
        public string Gas { get; set; }

        [JsonProperty(PropertyName = "net_fee")]
        public string NetFee { get; set; }

        [JsonProperty(PropertyName = "valid_until_block")]
        public uint ValidUntilBlock { get; set; }

        [JsonProperty(PropertyName = "attributes")]
        public AttrJson[] Attributes { get; set; }

        [JsonProperty(PropertyName = "witness")]
        public WitnessJson Witness { get; set; }
    }

    public class AttrJson
    {
        [JsonProperty(PropertyName = "usage")]
        public byte Usage { get; set; }

        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }
    }

}
