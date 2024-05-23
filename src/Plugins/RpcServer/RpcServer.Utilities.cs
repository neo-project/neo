// Copyright (C) 2015-2024 The Neo Project.
//
// RpcServer.Utilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.Wallets;
using System.Linq;

namespace Neo.Plugins
{
    partial class RpcServer
    {
        [RpcMethod]
        protected virtual JToken ListPlugins(JArray _params)
        {
            return new JArray(Plugin.Plugins
                .OrderBy(u => u.Name)
                .Select(u => new JObject
                {
                    ["name"] = u.Name,
                    ["version"] = u.Version.ToString(),
                    ["interfaces"] = new JArray(u.GetType().GetInterfaces()
                        .Select(p => p.Name)
                        .Where(p => p.EndsWith("Plugin"))
                        .Select(p => (JToken)p))
                }));
        }

        [RpcMethod]
        protected virtual JToken ValidateAddress(JArray _params)
        {
            string address = Result.Ok_Or(() => _params[0].AsString(), RpcError.InvalidParams.WithData($"Invlid address format: {_params[0]}"));
            JObject json = new();
            UInt160 scriptHash;
            try
            {
                scriptHash = address.ToScriptHash(system.Settings.AddressVersion);
            }
            catch
            {
                scriptHash = null;
            }
            json["address"] = address;
            json["isvalid"] = scriptHash != null;
            return json;
        }
    }
}
