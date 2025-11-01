// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.IEventHandlers;
using Neo.Json;
using Neo.Network.P2P;
using Neo.Plugins;
using Neo.Plugins.RpcServer.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Address = Neo.Plugins.RpcServer.Model.Address;

namespace Neo.Plugins.RpcServer
{
    public partial class RpcServer : IDisposable
    {
        private const int MaxParamsDepth = 32;
        private const string HttpMethodGet = "GET";
        private const string HttpMethodPost = "POST";

        internal record struct RpcParameter(string Name, Type Type, bool Required, object? DefaultValue);

        private record struct RpcMethod(Delegate Delegate, RpcParameter[] Parameters);

        private readonly Dictionary<string, RpcMethod> _methods = new();

        private IWebHost? host;
        private RpcServersSettings settings;
        private readonly NeoSystem system;
        private readonly LocalNode localNode;

        // avoid GetBytes every time
        private readonly byte[] _rpcUser;
        private readonly byte[] _rpcPass;

        public RpcServer(NeoSystem system, RpcServersSettings settings)
        {
            this.system = system;
            this.settings = settings;

            _rpcUser = string.IsNullOrEmpty(settings.RpcUser) ? [] : Encoding.UTF8.GetBytes(settings.RpcUser);
            _rpcPass = string.IsNullOrEmpty(settings.RpcPass) ? [] : Encoding.UTF8.GetBytes(settings.RpcPass);

            var addressVersion = system.Settings.AddressVersion;
            ParameterConverter.RegisterConversion<SignersAndWitnesses>(token => token.ToSignersAndWitnesses(addressVersion));

            // An address can be either UInt160 or Base58Check format.
            // If only UInt160 format is allowed, use UInt160 as parameter type.
            ParameterConverter.RegisterConversion<Address>(token => token.ToAddress(addressVersion));
            ParameterConverter.RegisterConversion<Address[]>(token => token.ToAddresses(addressVersion));

            localNode = system.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;
            RegisterMethods(this);
            Initialize_SmartContract();
        }

        internal bool CheckAuth(HttpContext context)
        {
            if (string.IsNullOrEmpty(settings.RpcUser)) return true;

            string? reqauth = context.Request.Headers.Authorization;
            if (string.IsNullOrEmpty(reqauth))
            {
                context.Response.Headers.WWWAuthenticate = "Basic realm=\"Restricted\"";
                context.Response.StatusCode = 401;
                return false;
            }

            byte[] auths;
            try
            {
                auths = Convert.FromBase64String(reqauth.Replace("Basic ", "").Trim());
            }
            catch
            {
                return false;
            }

            int colonIndex = Array.IndexOf(auths, (byte)':');
            if (colonIndex == -1) return false;

            var user = auths[..colonIndex];
            var pass = auths[(colonIndex + 1)..];

            // Always execute both checks, but both must evaluate to true
            return CryptographicOperations.FixedTimeEquals(user, _rpcUser) & CryptographicOperations.FixedTimeEquals(pass, _rpcPass);
        }

        private static JObject CreateErrorResponse(JToken? id, RpcError rpcError)
        {
            var response = CreateResponse(id);
            response["error"] = rpcError.ToJson();
            return response;
        }

        private static JObject CreateResponse(JToken? id)
        {
            return new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id
            };
        }

        /// <summary>
        /// Unwraps an exception to get the original exception.
        /// This is particularly useful for TargetInvocationException and AggregateException which wrap the actual exception.
        /// </summary>
        /// <param name="ex">The exception to unwrap</param>
        /// <returns>The unwrapped exception</returns>
        private static Exception UnwrapException(Exception ex)
        {
            if (ex is TargetInvocationException targetEx && targetEx.InnerException != null)
                return targetEx.InnerException;

            // Also handle AggregateException with a single inner exception
            if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count == 1)
                return aggEx.InnerExceptions[0];

            return ex;
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
                    if (settings.TrustedAuthorities is null || settings.TrustedAuthorities.Length == 0) return;
                    httpsConnectionAdapterOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpsConnectionAdapterOptions.ClientCertificateValidation = (cert, chain, err) =>
                    {
                        if (chain is null || err != SslPolicyErrors.None) return false;
                        var authority = chain.ChainElements[^1].Certificate;
                        return settings.TrustedAuthorities.Contains(authority.Thumbprint);
                    };
                });
            }))
            .Configure(app =>
            {
                if (settings.EnableCors) app.UseCors("All");
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                if (settings.EnableCors)
                {
                    if (settings.AllowOrigins.Length == 0)
                    {
                        services.AddCors(options =>
                        {
                            options.AddPolicy("All", policy =>
                            {
                                policy.AllowAnyOrigin()
                                    .WithHeaders("Content-Type")
                                    .WithMethods(HttpMethodGet, HttpMethodPost);
                                // The CORS specification states that setting origins to "*" (all origins)
                                // is invalid if the Access-Control-Allow-Credentials header is present.
                            });
                        });
                    }
                    else
                    {
                        services.AddCors(options =>
                        {
                            options.AddPolicy("All", policy =>
                            {
                                policy.WithOrigins(settings.AllowOrigins)
                                    .WithHeaders("Content-Type")
                                    .AllowCredentials()
                                    .WithMethods(HttpMethodGet, HttpMethodPost);
                            });
                        });
                    }
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

        internal void UpdateSettings(RpcServersSettings settings)
        {
            this.settings = settings;
        }

        public async Task ProcessAsync(HttpContext context)
        {
            if (context.Request.Method != HttpMethodGet && context.Request.Method != HttpMethodPost) return;

            JToken? request = null;
            if (context.Request.Method == HttpMethodGet)
            {
                string? jsonrpc = context.Request.Query["jsonrpc"];
                string? id = context.Request.Query["id"];
                string? method = context.Request.Query["method"];
                string? _params = context.Request.Query["params"];
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
            else if (context.Request.Method == HttpMethodPost)
            {
                using var reader = new StreamReader(context.Request.Body);
                try
                {
                    request = JToken.Parse(await reader.ReadToEndAsync(), MaxParamsDepth);
                }
                catch (FormatException) { }
            }

            JToken? response;
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
                    var tasks = array.Select(p => ProcessRequestAsync(context, (JObject?)p));
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

        internal async Task<JObject?> ProcessRequestAsync(HttpContext context, JObject? request)
        {
            var methodHint = request?["method"]?.AsString() ?? "unknown";
            PublishRpcTelemetry(new RpcTelemetryEventArgs(RpcTelemetryEventType.Started, methodHint));
            var stopwatch = Stopwatch.StartNew();

            if (request is null)
            {
                var errorResponse = CreateErrorResponse(null, RpcError.InvalidRequest);
                return CompleteRpcRequest(methodHint, stopwatch, errorResponse);
            }

            if (!request.ContainsProperty("id"))
            {
                return CompleteRpcRequest(methodHint, stopwatch, null);
            }

            var paramsToken = request["params"] ?? new JArray();
            if (paramsToken is not JArray jsonParameters)
            {
                var errorResponse = CreateErrorResponse(request["id"], RpcError.InvalidRequest);
                return CompleteRpcRequest(methodHint, stopwatch, errorResponse);
            }

            var method = request["method"]?.AsString();
            if (method is null)
            {
                var errorResponse = CreateErrorResponse(request["id"], RpcError.InvalidRequest);
                return CompleteRpcRequest(methodHint, stopwatch, errorResponse);
            }

            var methodName = method;
            var response = CreateResponse(request["id"]);
            try
            {
                (CheckAuth(context) && !settings.DisabledMethods.Contains(methodName)).True_Or(RpcError.AccessDenied);

                if (_methods.TryGetValue(methodName, out var rpcMethod))
                {
                    response["result"] = ProcessParamsMethod(jsonParameters, rpcMethod) switch
                    {
                        JToken result => result,
                        Task<JToken> task => await task,
                        _ => throw new NotSupportedException()
                    };
                    return CompleteRpcRequest(methodName, stopwatch, response);
                }

                throw new RpcException(RpcError.MethodNotFound.WithData(methodName));
            }
            catch (FormatException ex)
            {
                var errorResponse = CreateErrorResponse(request["id"], RpcError.InvalidParams.WithData(ex.Message));
                return CompleteRpcRequest(methodName, stopwatch, errorResponse, ex);
            }
            catch (IndexOutOfRangeException ex)
            {
                var errorResponse = CreateErrorResponse(request["id"], RpcError.InvalidParams.WithData(ex.Message));
                return CompleteRpcRequest(methodName, stopwatch, errorResponse, ex);
            }
            catch (Exception ex) when (ex is not RpcException)
            {
                var unwrapped = UnwrapException(ex);
#if DEBUG
                var errorResponse = CreateErrorResponse(request["id"],
                    RpcErrorFactory.NewCustomError(unwrapped.HResult, unwrapped.Message, unwrapped.StackTrace ?? string.Empty));
#else
                var errorResponse = CreateErrorResponse(request["id"], RpcErrorFactory.NewCustomError(unwrapped.HResult, unwrapped.Message));
#endif
                return CompleteRpcRequest(methodName, stopwatch, errorResponse, unwrapped);
            }
            catch (RpcException ex)
            {
#if DEBUG
                var errorResponse = CreateErrorResponse(request["id"], RpcErrorFactory.NewCustomError(ex.HResult, ex.Message, ex.StackTrace ?? string.Empty));
#else
                var errorResponse = CreateErrorResponse(request["id"], ex.GetError());
#endif
                return CompleteRpcRequest(methodName, stopwatch, errorResponse, ex);
            }
        }

        private static void PublishRpcTelemetry(RpcTelemetryEventArgs args)
        {
            foreach (var handler in Plugin.Plugins.OfType<IRpcDiagnosticsHandler>())
            {
                try
                {
                    handler.OnRpcTelemetry(args);
                }
                catch
                {
                    // Telemetry hooks must stay transparent to RPC behaviour.
                }
            }
        }

        private static JObject? CompleteRpcRequest(string method, Stopwatch stopwatch, JObject? response, Exception? exception = null)
        {
            stopwatch.Stop();
            var success = response is null || response["error"] is null;
            int? errorCode = null;
            string? errorMessage = null;

            if (!success && response?["error"] is JObject error)
            {
                if (error["code"] is JToken codeToken)
                {
                    try
                    {
                        errorCode = (int)codeToken.AsNumber();
                    }
                    catch
                    {
                        errorCode = null;
                    }
                }

                errorMessage = error["message"]?.AsString();
            }

            if (exception != null && string.IsNullOrEmpty(errorMessage))
                errorMessage = exception.Message;

            PublishRpcTelemetry(new RpcTelemetryEventArgs(
                RpcTelemetryEventType.Completed,
                method,
                stopwatch.Elapsed,
                success,
                errorCode,
                errorMessage));

            return response;
        }

        private object? ProcessParamsMethod(JArray arguments, RpcMethod rpcMethod)
        {
            var args = new object?[rpcMethod.Parameters.Length];

            // If the method has only one parameter of type JArray, invoke the method directly with the arguments
            if (rpcMethod.Parameters.Length == 1 && rpcMethod.Parameters[0].Type == typeof(JArray))
            {
                return rpcMethod.Delegate.DynamicInvoke(arguments);
            }

            for (var i = 0; i < rpcMethod.Parameters.Length; i++)
            {
                var param = rpcMethod.Parameters[i];
                if (arguments.Count > i && arguments[i] is not null) // Donot parse null values
                {
                    try
                    {
                        args[i] = ParameterConverter.AsParameter(arguments[i]!, param.Type);
                    }
                    catch (Exception e) when (e is not RpcException)
                    {
                        throw new ArgumentException($"Invalid value for parameter '{param.Name}'", e);
                    }
                }
                else
                {
                    if (param.Required)
                        throw new ArgumentException($"Required parameter '{param.Name}' is missing");
                    args[i] = param.DefaultValue;
                }
            }

            return rpcMethod.Delegate.DynamicInvoke(args);
        }

        public void RegisterMethods(object handler)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var method in handler.GetType().GetMethods(flags))
            {
                var rpcMethod = method.GetCustomAttribute<RpcMethodAttribute>();
                if (rpcMethod is null) continue;

                var name = string.IsNullOrEmpty(rpcMethod.Name) ? method.Name.ToLowerInvariant() : rpcMethod.Name;
                var delegateParams = method.GetParameters()
                    .Select(p => p.ParameterType)
                    .Concat([method.ReturnType])
                    .ToArray();
                var delegateType = Expression.GetDelegateType(delegateParams);

                _methods[name] = new RpcMethod(
                    Delegate.CreateDelegate(delegateType, handler, method),
                    method.GetParameters().Select(AsRpcParameter).ToArray()
                );
            }
        }

        static internal RpcParameter AsRpcParameter(ParameterInfo param)
        {
            // Required if not optional and not nullable
            // For reference types, if parameter has not default value and nullable is disabled, it is optional.
            // For value types, if parameter has not default value, it is required.
            var required = param.IsOptional ? false : NotNullParameter(param);
            return new RpcParameter(param.Name ?? string.Empty, param.ParameterType, required, param.DefaultValue);
        }

        static private bool NotNullParameter(ParameterInfo param)
        {
            if (param.GetCustomAttribute<NotNullAttribute>() != null) return true;
            if (param.GetCustomAttribute<DisallowNullAttribute>() != null) return true;

            if (param.GetCustomAttribute<AllowNullAttribute>() != null) return false;
            if (param.GetCustomAttribute<MaybeNullAttribute>() != null) return false;

            var context = new NullabilityInfoContext();
            var nullabilityInfo = context.Create(param);
            return nullabilityInfo.WriteState == NullabilityState.NotNull;
        }
    }
}
