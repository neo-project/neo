// Copyright (C) 2015-2024 The Neo Project.
//
// RestServerMiddleware.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.Plugins.RestServer.Middleware
{
    internal class RestServerMiddleware
    {
        private readonly RequestDelegate _next;

        public RestServerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            SetServerInfomationHeader(response);

            await _next(context);
        }

        public static void SetServerInfomationHeader(HttpResponse response)
        {
            var neoCliAsm = Assembly.GetEntryAssembly()?.GetName();
            var restServerAsm = Assembly.GetExecutingAssembly().GetName();

            if (neoCliAsm?.Version is not null && restServerAsm.Version is not null)
            {
                if (restServerAsm.Version is not null)
                {
                    response.Headers.Server = $"{neoCliAsm.Name}/{neoCliAsm.Version.ToString(3)} {restServerAsm.Name}/{restServerAsm.Version.ToString(3)}";
                }
                else
                {
                    response.Headers.Server = $"{neoCliAsm.Name}/{neoCliAsm.Version.ToString(3)} {restServerAsm.Name}";
                }
            }
            else
            {
                if (neoCliAsm is not null)
                {
                    if (restServerAsm is not null)
                    {
                        response.Headers.Server = $"{neoCliAsm.Name} {restServerAsm.Name}";
                    }
                    else
                    {
                        response.Headers.Server = $"{neoCliAsm.Name}";
                    }
                }
                else
                {
                    // Can't get the server name/version
                }
            }
        }
    }
}
