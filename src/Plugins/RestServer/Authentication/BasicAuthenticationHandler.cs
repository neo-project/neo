// Copyright (C) 2015-2024 The Neo Project.
//
// BasicAuthenticationHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo.Plugins.RestServer;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace RestServer.Authentication
{
    internal class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Request.Headers.Authorization;
            if (string.IsNullOrEmpty(authHeader) == false && AuthenticationHeaderValue.TryParse(authHeader, out var authValue))
            {
                if (authValue.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) && authValue.Parameter != null)
                {
                    try
                    {
                        var decodedParams = Encoding.UTF8.GetString(Convert.FromBase64String(authValue.Parameter));
                        var creds = decodedParams.Split(':', 2);

                        if (creds.Length == 2 && creds[0] == RestServerSettings.Current.RestUser && creds[1] == RestServerSettings.Current.RestPass)
                        {
                            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, creds[0]) };
                            var identity = new ClaimsIdentity(claims, Scheme.Name);
                            var principal = new ClaimsPrincipal(identity);
                            var ticket = new AuthenticationTicket(principal, Scheme.Name);

                            return Task.FromResult(AuthenticateResult.Success(ticket));
                        }
                    }
                    catch (FormatException)
                    {
                    }
                }
            }
            return Task.FromResult(AuthenticateResult.Fail("Authentication Failed!!!"));
        }
    }
}
