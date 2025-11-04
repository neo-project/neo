// Copyright (C) 2015-2025 The Neo Project.
//
// NEP6Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract;
using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace Neo.Wallets.NEP6
{
    internal class NEP6Contract : Contract
    {
        public string[] ParameterNames;
        public bool Deployed;

        public static NEP6Contract FromJson(JsonObject json)
        {
            if (json == null) return null;
            return new NEP6Contract
            {
                Script = Convert.FromBase64String(json["script"].AsString()),
                ParameterList = ((JsonArray)json["parameters"]).Select(p => p["type"].GetEnum<ContractParameterType>()).ToArray(),
                ParameterNames = ((JsonArray)json["parameters"]).Select(p => p["name"].AsString()).ToArray(),
                Deployed = json["deployed"].GetValue<bool>()
            };
        }

        public JsonObject ToJson()
        {
            return new()
            {
                ["script"] = Convert.ToBase64String(Script),
                ["parameters"] = new JsonArray(ParameterList.Zip(ParameterNames, (type, name) => new JsonObject()
                {
                    ["name"] = name,
                    ["type"] = type.ToString()
                }).ToArray()),
                ["deployed"] = Deployed
            };
        }
    }
}
