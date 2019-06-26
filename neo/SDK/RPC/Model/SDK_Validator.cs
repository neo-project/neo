using Neo.IO.Json;
using System.Numerics;

namespace Neo.SDK.RPC.Model
{
    public class SDK_Validator
    {
        public string PublicKey { get; set; }

        public BigInteger Votes { get; set; }

        public bool Active { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["publickey"] = PublicKey;
            json["votes"] = Votes.ToString();
            json["active"] = Active;
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
