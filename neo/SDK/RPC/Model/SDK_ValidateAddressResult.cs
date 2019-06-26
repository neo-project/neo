using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_ValidateAddressResult
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "isvalid")]
        public bool IsValid { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["address"] = Address;
            json["isvalid"] = IsValid.ToString();
            return json;
        }

        public static SDK_ValidateAddressResult FromJson(JObject json)
        {
            SDK_ValidateAddressResult validateAddress = new SDK_ValidateAddressResult();
            validateAddress.Address = json["address"].AsString();
            validateAddress.IsValid = json["isvalid"].AsBoolean();
            return validateAddress;
        }
    }

}
