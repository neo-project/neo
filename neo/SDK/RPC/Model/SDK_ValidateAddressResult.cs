using Neo.IO.Json;

namespace Neo.SDK.RPC.Model
{
    public class SDK_ValidateAddressResult
    {
        public string Address { get; set; }
        
        public bool IsValid { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["address"] = Address;
            json["isvalid"] = IsValid;
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
