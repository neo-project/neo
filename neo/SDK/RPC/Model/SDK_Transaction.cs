using Neo.IO.Json;
using Newtonsoft.Json;
using System.Linq;

public class SDK_Transaction
{
    [JsonProperty(PropertyName = "hash")]
    public string Hash { get; set; }

    [JsonProperty(PropertyName = "size")]
    public uint Size { get; set; }

    [JsonProperty(PropertyName = "version")]
    public uint Version { get; set; }

    [JsonProperty(PropertyName = "nonce")]
    public uint Nonce { get; set; }

    [JsonProperty(PropertyName = "sender")]
    public string Sender { get; set; }

    [JsonProperty(PropertyName = "sys_fee")]
    public string SysFee { get; set; }

    [JsonProperty(PropertyName = "net_fee")]
    public string NetFee { get; set; }

    [JsonProperty(PropertyName = "valid_until_block")]
    public uint ValidUntilBlock { get; set; }

    [JsonProperty(PropertyName = "attributes")]
    public SDK_TransactionAttribute[] Attributes { get; set; }

    [JsonProperty(PropertyName = "script")]
    public string Script { get; set; }

    [JsonProperty(PropertyName = "witnesses")]
    public SDK_Witness[] Witnesses { get; set; }

    public static SDK_Transaction FromJson(JObject json)
    {
        SDK_Transaction tx = new SDK_Transaction();
        tx.Hash = json["hash"].AsString();
        //transaction.TxId = json[]
        tx.Size = uint.Parse(json["size"].AsString());
        tx.Version = uint.Parse(json["version"].AsString());
        tx.Nonce = uint.Parse(json["nonce"].AsString());
        tx.Sender = json["sender"].AsString();
        tx.SysFee = json["sys_fee"].AsString();
        tx.NetFee = json["net_fee"].AsString();
        tx.ValidUntilBlock = uint.Parse(json["valid_until_block"].AsString());
        tx.Attributes = ((JArray)json["attributes"]).Select(p => SDK_TransactionAttribute.FromJson(p)).ToArray();
        tx.Script = json["script"].AsString();
        tx.Witnesses = ((JArray)json["witnesses"]).Select(p => SDK_Witness.FromJson(p)).ToArray();

        return tx;
    }
}

public class SDK_TransactionAttribute
{
    [JsonProperty(PropertyName = "usage")]
    public byte Usage { get; set; }

    [JsonProperty(PropertyName = "data")]
    public string Data { get; set; }

    public static SDK_TransactionAttribute FromJson(JObject json)
    {
        SDK_TransactionAttribute transactionAttribute = new SDK_TransactionAttribute();
        transactionAttribute.Usage = byte.Parse(json["usage"].AsString());
        transactionAttribute.Data = json["data"].AsString();
        return transactionAttribute;
    }
}

public class SDK_Witness
{
    [JsonProperty(PropertyName = "invocation")]
    public string Invocation { get; set; }

    [JsonProperty(PropertyName = "verification")]
    public string Verification { get; set; }

    public static SDK_Witness FromJson(JObject json)
    {
        SDK_Witness witness = new SDK_Witness();
        witness.Invocation = json["invocation"].AsString();
        witness.Verification = json["verification"].AsString();
        return witness;
    }
}