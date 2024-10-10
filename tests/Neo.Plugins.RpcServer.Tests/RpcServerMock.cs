// Copyright (C) 2015-2024 The Neo Project.
//
// RpcServerMock.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Neo.Json;
using System.Threading.Tasks;

namespace Neo.Plugins.RpcServer.Tests;

public class RpcServerMock(NeoSystem system, RpcServerSettings settings) : RpcServer(system, settings)
{
    public async Task<JObject> ProcessRequestMock(HttpContext context, string method, JArray parameters)
    {
        var request = new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = method,
            ["params"] = parameters
        };

        var res = await ProcessRequestAsync(context, request);
        return (JObject)res["result"];
    }
}
