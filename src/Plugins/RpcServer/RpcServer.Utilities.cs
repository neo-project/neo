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

using Neo.Wallets;
using System.Linq;
using System.Text.Json.Nodes;

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
        protected internal virtual JsonNode ListPlugins()
        {
            return new JsonArray(Plugin.Plugins
                .OrderBy(u => u.Name)
                .Select(u => new JsonObject
                {
                    ["name"] = u.Name,
                    ["version"] = u.Version.ToString(),
                    ["interfaces"] = new JsonArray(u.GetType().GetInterfaces()
                        .Select(p => p.Name)
                        .Where(p => p.EndsWith("Plugin"))
                        .Select(p => (JsonNode)p)
                        .ToArray())
                }).ToArray());
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
        protected internal virtual JsonNode ValidateAddress(string address)
        {
            UInt160? scriptHash;
            try
            {
                scriptHash = address.ToScriptHash(system.Settings.AddressVersion);
            }
            catch
            {
                scriptHash = null;
            }

            return new JsonObject()
            {
                ["address"] = address,
                ["isvalid"] = scriptHash != null,
            };
        }
    }
}
