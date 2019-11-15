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

        public static ContractContactInformation FromJson(JObject jsonObject)
        {
            return jsonObject != null ? new ContractContactInformation
            {
                Author = jsonObject["author"]?.AsString(),
                Email = jsonObject["email"]?.AsString(),
            } : new ContractContactInformation();
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["author"] = Author;
            json["email"] = Email;
            return json;
        }
    }
}
