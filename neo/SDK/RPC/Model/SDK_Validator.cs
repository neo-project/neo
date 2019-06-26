using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_Validator
    {
        [JsonProperty(PropertyName = "publickey")]
        public string PublicKey { get; set; }

        [JsonProperty(PropertyName = "votes")]
        public BigInteger Votes { get; set; }

        [JsonProperty(PropertyName = "active")]
        public bool Active { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["publickey"] = PublicKey;
            json["votes"] = Votes.ToString();
            json["active"] = Active.ToString();
            return json;
        }

        public static SDK_Validator FromJson(JObject json)
        {
            SDK_Validator validator = new SDK_Validator();
            validator.PublicKey = json["publickey"].AsString();
            validator.Votes = BigInteger.Parse(json["votes"].AsString());
            validator.Active = json["active"].AsBoolean();
            return validator;
        }
    }
}
