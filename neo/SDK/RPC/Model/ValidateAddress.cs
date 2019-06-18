using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class ValidateAddress
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "isvalid")]
        public bool IsValid { get; set; }

    }

}
