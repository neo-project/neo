// Copyright (C) 2015-2025 The Neo Project.
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

namespace Neo.Plugins.RpcServer
{
    partial class RpcServer
    {
        /// <summary>
        /// Lists all plugins.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "listplugins"}</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": [
        ///     {"name": "The plugin name", "version": "The plugin version", "interfaces": ["The plugin method name"]}
        ///   ]
        /// }</code>
        /// </summary>
        /// <returns>A JSON array containing the plugin information.</returns>
        [RpcMethod]
        protected internal virtual JToken ListPlugins()
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

        /// <summary>
        /// Validates an address.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "method": "validateaddress", "params": ["The Base58Check address"]}</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {"address": "The Base58Check address", "isvalid": true}
        /// }</code>
        /// </summary>
        /// <param name="address">The address as a string.</param>
        /// <returns>A JSON object containing the address and whether it is valid.</returns>
        [RpcMethod]
        protected internal virtual JToken ValidateAddress(string address)
        {
            UInt160 scriptHash;
            try
            {
                scriptHash = address.ToScriptHash(system.Settings.AddressVersion);
            }
            catch
            {
                scriptHash = null;
            }

            return new JObject()
            {
                ["address"] = address,
                ["isvalid"] = scriptHash != null,
            };
        }
    }
}
