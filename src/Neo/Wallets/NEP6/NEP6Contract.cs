// Copyright (C) 2015-2024 The Neo Project.
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

namespace Neo.Wallets.NEP6
{
    internal class NEP6Contract : Contract
    {
        public string[] ParameterNames;
        public bool Deployed;

        public static NEP6Contract FromJson(JObject json)
        {
            if (json == null) return null;
            try
            {
                return new NEP6Contract
                {
                    Script = Convert.FromBase64String(json["script"].AsString()),
                    ParameterList = ((JArray)json["parameters"]).Select(p => p["type"].GetEnum<ContractParameterType>()).ToArray(),
                    ParameterNames = ((JArray)json["parameters"]).Select(p => p["name"].AsString()).ToArray(),
                    Deployed = json["deployed"].AsBoolean()
                };
            }
            catch (Exception e)
            {
                throw WalletException.FromException(e);
            }
        }

        public JObject ToJson()
        {
            try
            {
                JObject contract = new()
                {
                    ["script"] = Convert.ToBase64String(Script),
                    ["parameters"] = new JArray(ParameterList.Zip(ParameterNames, (type, name) =>
                    {
                        JObject parameter = new() { ["name"] = name, ["type"] = type };
                        return parameter;
                    })),
                    ["deployed"] = Deployed
                };
                return contract;
            }
            catch (Exception e)
            {
                throw WalletException.FromException(e);
            }
        }
    }
}
