using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.Manifest
{
    public class ContractContactInformation
    {
        public string Author { get; set; } = "";

        public string Email { get; set; } = "";

        public string Description { get; set; } = "";

        public string Website { get; set; } = "";

        public static ContractContactInformation FromJson(JObject jsonObject)
        {
            return jsonObject != null ? new ContractContactInformation
            {
                Author = jsonObject["author"]?.AsString(),
                Email = jsonObject["email"]?.AsString(),
                Description = jsonObject["description"]?.AsString(),
                Website = jsonObject["website"]?.AsString(),
            } : new ContractContactInformation();
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["author"] = Author;
            json["email"] = Email;
            json["description"] = Description;
            json["website"] = Website;
            return json;
        }
    }
}
