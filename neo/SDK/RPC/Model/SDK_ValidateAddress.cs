using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_ValidateAddress
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "isvalid")]
        public bool IsValid { get; set; }

        public static SDK_ValidateAddress FromJson(JObject json)
        {
            SDK_ValidateAddress validateAddress = new SDK_ValidateAddress();
            validateAddress.Address = json["address"].AsString();
            validateAddress.IsValid = json["isvalid"].AsBoolean();
            return validateAddress;
        }
    }

}
