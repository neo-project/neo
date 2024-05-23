// Copyright (C) 2015-2024 The Neo Project.
//
// RpcServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Neo.Json;
using Neo.Network.P2P;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins
{
    public partial class RpcServer : IDisposable
    {
        private const int MaxParamsDepth = 32;

        private readonly Dictionary<string, Func<JArray, object>> methods = new();

        private IWebHost host;
        private RpcServerSettings settings;
        private readonly NeoSystem system;
        private readonly LocalNode localNode;

        public RpcServer(NeoSystem system, RpcServerSettings settings)
        {
            this.system = system;
            this.settings = settings;
            localNode = system.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;
            RegisterMethods(this);
            Initialize_SmartContract();
        }

        private bool CheckAuth(HttpContext context)
        {
            if (string.IsNullOrEmpty(settings.RpcUser)) return true;

            string reqauth = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(reqauth))
            {
                context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Restricted\"";
                context.Response.StatusCode = 401;
                return false;
            }

            string authstring;
            try
            {
                authstring = Encoding.UTF8.GetString(Convert.FromBase64String(reqauth.Replace("Basic ", "").Trim()));
            }
            catch
            {
                return false;
            }

            string[] authvalues = authstring.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (authvalues.Length < 2)
                return false;

            return authvalues[0] == settings.RpcUser && authvalues[1] == settings.RpcPass;
        }

        private static JObject CreateErrorResponse(JToken id, RpcError rpcError)
        {
            JObject response = CreateResponse(id);
            response["error"] = rpcError.ToJson();
            return response;
        }

        private static JObject CreateResponse(JToken id)
        {
            JObject response = new();
            response["jsonrpc"] = "2.0";
            response["id"] = id;
            return response;
        }

        public void Dispose()
        {
            Dispose_SmartContract();
            if (host != null)
            {
                host.Dispose();
                host = null;
            }
        }

        public void StartRpcServer()
        {
            host = new WebHostBuilder().UseKestrel(options => options.Listen(settings.BindAddress, settings.Port, listenOptions =>
            {
                // Default value is 5Mb
                options.Limits.MaxRequestBodySize = settings.MaxRequestBodySize;
                options.Limits.MaxRequestLineSize = Math.Min(settings.MaxRequestBodySize, options.Limits.MaxRequestLineSize);
                // Default value is 40
                options.Limits.MaxConcurrentConnections = settings.MaxConcurrentConnections;

                // Default value is 1 minutes
                options.Limits.KeepAliveTimeout = settings.KeepAliveTimeout == -1 ?
                    TimeSpan.MaxValue :
                    TimeSpan.FromSeconds(settings.KeepAliveTimeout);

                // Default value is 15 seconds
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(settings.RequestHeadersTimeout);

                if (string.IsNullOrEmpty(settings.SslCert)) return;
                listenOptions.UseHttps(settings.SslCert, settings.SslCertPassword, httpsConnectionAdapterOptions =>
                {
                    if (settings.TrustedAuthorities is null || settings.TrustedAuthorities.Length == 0)
                        return;
                    httpsConnectionAdapterOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpsConnectionAdapterOptions.ClientCertificateValidation = (cert, chain, err) =>
                    {
                        if (err != SslPolicyErrors.None)
                            return false;
                        X509Certificate2 authority = chain.ChainElements[^1].Certificate;
                        return settings.TrustedAuthorities.Contains(authority.Thumbprint);
                    };
                });
            }))
            .Configure(app =>
            {
                if (settings.EnableCors)
                    app.UseCors("All");

                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                if (settings.EnableCors)
                {
                    if (settings.AllowOrigins.Length == 0)
                        services.AddCors(options =>
                        {
                            options.AddPolicy("All", policy =>
                            {
                                policy.AllowAnyOrigin()
                                .WithHeaders("Content-Type")
                                .WithMethods("GET", "POST");
                                // The CORS specification states that setting origins to "*" (all origins)
                                // is invalid if the Access-Control-Allow-Credentials header is present.
                            });
                        });
                    else
                        services.AddCors(options =>
                        {
                            options.AddPolicy("All", policy =>
                            {
                                policy.WithOrigins(settings.AllowOrigins)
                                .WithHeaders("Content-Type")
                                .AllowCredentials()
                                .WithMethods("GET", "POST");
                            });
                        });
                }

                services.AddResponseCompression(options =>
                {
                    // options.EnableForHttps = false;
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Append("application/json");
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            host.Start();
        }

        internal void UpdateSettings(RpcServerSettings settings)
        {
            this.settings = settings;
        }

        public async Task ProcessAsync(HttpContext context)
        {
            if (context.Request.Method != "GET" && context.Request.Method != "POST") return;
            JToken request = null;
            if (context.Request.Method == "GET")
            {
                string jsonrpc = context.Request.Query["jsonrpc"];
                string id = context.Request.Query["id"];
                string method = context.Request.Query["method"];
                string _params = context.Request.Query["params"];
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(method) && !string.IsNullOrEmpty(_params))
                {
                    try
                    {
                        _params = Encoding.UTF8.GetString(Convert.FromBase64String(_params));
                    }
                    catch (FormatException) { }
                    request = new JObject();
                    if (!string.IsNullOrEmpty(jsonrpc))
                        request["jsonrpc"] = jsonrpc;
                    request["id"] = id;
                    request["method"] = method;
                    request["params"] = JToken.Parse(_params, MaxParamsDepth);
                }
            }
            else if (context.Request.Method == "POST")
            {
                using StreamReader reader = new(context.Request.Body);
                try
                {
                    request = JToken.Parse(await reader.ReadToEndAsync(), MaxParamsDepth);
                }
                catch (FormatException) { }
            }
            JToken response;
            if (request == null)
            {
                response = CreateErrorResponse(null, RpcError.BadRequest);
            }
            else if (request is JArray array)
            {
                if (array.Count == 0)
                {
                    response = CreateErrorResponse(request["id"], RpcError.InvalidRequest);
                }
                else
                {
                    var tasks = array.Select(p => ProcessRequestAsync(context, (JObject)p));
                    var results = await Task.WhenAll(tasks);
                    response = results.Where(p => p != null).ToArray();
                }
            }
            else
            {
                response = await ProcessRequestAsync(context, (JObject)request);
            }
            if (response == null || (response as JArray)?.Count == 0) return;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(response.ToString(), Encoding.UTF8);
        }

        private async Task<JObject> ProcessRequestAsync(HttpContext context, JObject request)
        {
            if (!request.ContainsProperty("id")) return null;
            JToken @params = request["params"] ?? new JArray();
            if (!request.ContainsProperty("method") || @params is not JArray)
            {
                return CreateErrorResponse(request["id"], RpcError.InvalidRequest);
            }
            JObject response = CreateResponse(request["id"]);
            try
            {
                string method = request["method"].AsString();
                (CheckAuth(context) && !settings.DisabledMethods.Contains(method)).True_Or(RpcError.AccessDenied);
                methods.TryGetValue(method, out var func).True_Or(RpcErrorFactory.MethodNotFound(method));
                response["result"] = func((JArray)@params) switch
                {
                    JToken result => result,
                    Task<JToken> task => await task,
                    _ => throw new NotSupportedException()
                };
                return response;
            }
            catch (FormatException ex)
            {
                return CreateErrorResponse(request["id"], RpcError.InvalidParams.WithData(ex.Message));
            }
            catch (IndexOutOfRangeException ex)
            {
                return CreateErrorResponse(request["id"], RpcError.InvalidParams.WithData(ex.Message));
            }
            catch (Exception ex)
            {
#if DEBUG
                return CreateErrorResponse(request["id"], RpcErrorFactory.NewCustomError(ex.HResult, ex.Message, ex.StackTrace));
#else
                return CreateErrorResponse(request["id"], RpcErrorFactory.NewCustomError(ex.HResult, ex.Message));
#endif
            }
        }

        public void RegisterMethods(object handler)
        {
            foreach (MethodInfo method in handler.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                RpcMethodAttribute attribute = method.GetCustomAttribute<RpcMethodAttribute>();
                if (attribute is null) continue;
                string name = string.IsNullOrEmpty(attribute.Name) ? method.Name.ToLowerInvariant() : attribute.Name;
                methods[name] = method.CreateDelegate<Func<JArray, object>>(handler);
            }
        }
    }
}
