// Copyright (C) 2015-2025 The Neo Project.
//
// RpcDispatcher.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core;
using Neo.Build.Core.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Net
{
    internal class RpcDispatcher
    {
        private readonly Dictionary<string, Func<object?, Task<object?>>> _handlers = new();

        public void Register<TParams, TResult>(string method, Func<TParams?, Task<TResult?>> handler)
            where TParams : class?
        {
            _handlers[method] = async raw =>
            {
                var tp = JsonSerializer.Deserialize<TParams>(JsonSerializer.Serialize(raw));
                return await handler(tp);
            };
        }

        public bool Unregister(string method) =>
            _handlers.Remove(method);

        public async Task<object> DispatchAsync(JsonRpcRequest<object>? request)
        {
            if (request is not null)
            {
                if (string.IsNullOrEmpty(request.Method))
                    return JsonRpcError.CreateResponse(JsonRpcErrorCodes.InvalidRequest, "The JSON sent is not a valid Request object.");

                if (_handlers.TryGetValue(request.Method, out var handler) == false)
                    return JsonRpcError.CreateResponse(JsonRpcErrorCodes.MethodNotFound, "The method does not exist / is not available.");
                else
                {
                    var result = await handler(request.Params);
                    return new JsonRpcResponse<object> { Result = result, Id = request.Id };
                }
            }

            return JsonRpcError.CreateResponse(JsonRpcErrorCodes.InternalError, "Internal JSON-RPC error.");
        }
    }
}
