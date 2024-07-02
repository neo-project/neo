// Copyright (C) 2015-2024 The Neo Project.
//
// RestWebServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads.Conditions;
using Neo.Plugins.RestServer.Binder;
using Neo.Plugins.RestServer.Middleware;
using Neo.Plugins.RestServer.Models.Error;
using Neo.Plugins.RestServer.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestServer.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Net.Security;
using System.Numerics;

namespace Neo.Plugins.RestServer
{
    internal class RestWebServer
    {
        #region Globals

        private readonly RestServerSettings _settings;
        private IWebHost? _host;

        #endregion

        public static bool IsRunning { get; private set; }

        public RestWebServer()
        {
            _settings = RestServerSettings.Current;
        }

        public void Start()
        {
            if (IsRunning)
                return;

            IsRunning = true;

            _host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    // Web server configuration
                    options.AddServerHeader = false;
                    options.Limits.MaxConcurrentConnections = _settings.MaxConcurrentConnections;
                    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(_settings.KeepAliveTimeout);
                    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
                    options.Listen(_settings.BindAddress, unchecked((int)_settings.Port),
                        listenOptions =>
                        {
                            if (string.IsNullOrEmpty(_settings.SslCertFile))
                                return;
                            listenOptions.UseHttps(_settings.SslCertFile, _settings.SslCertPassword, httpsOptions =>
                            {
                                if (_settings.TrustedAuthorities.Length == 0)
                                {
                                    httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                                    httpsOptions.ClientCertificateValidation = (cert, chain, err) =>
                                    {
                                        if (chain is null || err != SslPolicyErrors.None)
                                            return false;
                                        var authority = chain.ChainElements[^1].Certificate;
                                        return _settings.TrustedAuthorities.Any(a => a.Equals(authority.Thumbprint, StringComparison.OrdinalIgnoreCase));
                                    };
                                }
                            });
                        });
                })
                .ConfigureServices(services =>
                {
                    #region Add Basic auth

                    if (_settings.EnableBasicAuthentication)
                        services.AddAuthentication()
                        .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

                    #endregion

                    #region CORS

                    // Server configuration
                    if (_settings.EnableCors)
                    {
                        if (_settings.AllowOrigins.Length == 0)
                            services.AddCors(options =>
                            {
                                options.AddPolicy("All", policy =>
                                {
                                    policy.AllowAnyOrigin()
                                    .AllowAnyHeader()
                                    .WithMethods("GET", "POST");
                                    // The CORS specification states that setting origins to "*" (all origins)
                                    // is invalid if the Access-Control-Allow-Credentials header is present.
                                    //.AllowCredentials() 
                                });
                            });
                        else
                            services.AddCors(options =>
                            {
                                options.AddPolicy("All", policy =>
                                {
                                    policy.WithOrigins(_settings.AllowOrigins)
                                    .AllowAnyHeader()
                                    .AllowCredentials()
                                    .WithMethods("GET", "POST");
                                });
                            });
                    }

                    #endregion

                    services.AddRouting(options => options.LowercaseUrls = options.LowercaseQueryStrings = true);

                    #region Compression Configuration

                    if (_settings.EnableCompression)
                        services.AddResponseCompression(options =>
                        {
                            options.EnableForHttps = false;
                            options.Providers.Add<GzipCompressionProvider>();
                            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Append(MediaTypeNames.Application.Json);
                        });

                    #endregion

                    #region Controllers

                    var controllers = services
                        .AddControllers(options =>
                        {
                            options.EnableEndpointRouting = false;

                            if (_settings.EnableBasicAuthentication)
                            {
                                var policy = new AuthorizationPolicyBuilder()
                                                .RequireAuthenticatedUser()
                                                .Build();
                                options.Filters.Add(new AuthorizeFilter(policy));
                            }
                            options.ModelBinderProviders.Insert(0, new NeoBinderProvider());
                        })
                        .ConfigureApiBehaviorOptions(options =>
                        {
                            options.InvalidModelStateResponseFactory = context =>
                                new BadRequestObjectResult(
                                    new ParameterFormatExceptionModel(string.Join(' ', context.ModelState.Values.SelectMany(s => s.Errors).Select(s => s.ErrorMessage))))
                                {
                                    ContentTypes =
                                        {
                                            MediaTypeNames.Application.Json,
                                        }
                                };
                        })
                        .ConfigureApplicationPartManager(manager =>
                        {
                            var controllerFeatureProvider = manager.FeatureProviders.Single(p => p.GetType() == typeof(ControllerFeatureProvider));
                            var index = manager.FeatureProviders.IndexOf(controllerFeatureProvider);
                            manager.FeatureProviders[index] = new BlackListControllerFeatureProvider();

                            foreach (var plugin in Plugin.Plugins)
                                manager.ApplicationParts.Add(new AssemblyPart(plugin.GetType().Assembly));
                        })
                        .AddNewtonsoftJson(options =>
                        {
                            options.AllowInputFormatterExceptionMessages = true;
                            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                            options.SerializerSettings.Formatting = Formatting.None;

                            foreach (var converter in _settings.JsonSerializerSettings.Converters)
                                options.SerializerSettings.Converters.Add(converter);
                        });

                    #endregion

                    #region API Versioning

                    services.AddVersionedApiExplorer(setupAction =>
                    {
                        setupAction.GroupNameFormat = "'v'VV";
                    });

                    services.AddApiVersioning(options =>
                    {
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.DefaultApiVersion = new ApiVersion(1, 0);
                        options.ReportApiVersions = true;
                    });

                    #endregion

                    #region Swagger Configuration

                    if (_settings.EnableSwagger)
                    {
                        var apiVersionDescriptionProvider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
                        services.AddSwaggerGen(options =>
                        {
                            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                            {
                                options.SwaggerDoc(description.GroupName, new OpenApiInfo()
                                {
                                    Title = "RestServer Plugin API",
                                    Description = "RESTful Web Sevices for neo-cli.",
                                    Version = description.ApiVersion.ToString(),
                                    Contact = new OpenApiContact()
                                    {
                                        Name = "The Neo Project",
                                        Url = new Uri("https://github.com/neo-project/neo-modules"),
                                        Email = "dev@neo.org",
                                    },
                                    License = new OpenApiLicense()
                                    {
                                        Name = "MIT",
                                        Url = new Uri("http://www.opensource.org/licenses/mit-license.php"),
                                    },
                                });
                            }

                            #region Enable Basic Auth for Swagger

                            if (_settings.EnableBasicAuthentication)
                            {
                                options.AddSecurityDefinition("basicAuth", new OpenApiSecurityScheme()
                                {
                                    Type = SecuritySchemeType.Http,
                                    Scheme = "basic",
                                    Description = "Input your username and password to access this API.",
                                });
                                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                                {
                                    {
                                        new OpenApiSecurityScheme()
                                        {
                                            Reference = new OpenApiReference()
                                            {
                                                Type = ReferenceType.SecurityScheme,
                                                Id = "basicAuth",
                                            },
                                        },
                                        new List<string>()
                                    }
                                });
                            }

                            #endregion

                            options.DocInclusionPredicate((docmentName, apiDescription) =>
                            {
                                var actionApiVersionModel = apiDescription.ActionDescriptor.GetApiVersionModel(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);
                                if (actionApiVersionModel == null)
                                    return true;
                                if (actionApiVersionModel.DeclaredApiVersions.Any())
                                    return actionApiVersionModel.DeclaredApiVersions.Any(a => $"v{a}" == docmentName);
                                return actionApiVersionModel.ImplementedApiVersions.Any(a => $"v{a}" == docmentName);
                            });

                            options.UseOneOfForPolymorphism();
                            options.SelectSubTypesUsing(baseType =>
                            {
                                if (baseType == typeof(WitnessCondition))
                                {
                                    return new[]
                                    {
                                        typeof(BooleanCondition),
                                        typeof(NotCondition),
                                        typeof(AndCondition),
                                        typeof(OrCondition),
                                        typeof(ScriptHashCondition),
                                        typeof(GroupCondition),
                                        typeof(CalledByEntryCondition),
                                        typeof(CalledByContractCondition),
                                        typeof(CalledByGroupCondition),
                                    };
                                }

                                return Enumerable.Empty<Type>();
                            });
                            options.MapType<UInt256>(() => new OpenApiSchema()
                            {
                                Type = "string",
                                Format = "hash256",
                            });
                            options.MapType<UInt160>(() => new OpenApiSchema()
                            {
                                Type = "string",
                                Format = "hash160",
                            });
                            options.MapType<ECPoint>(() => new OpenApiSchema()
                            {
                                Type = "string",
                                Format = "hexstring",
                            });
                            options.MapType<BigInteger>(() => new OpenApiSchema()
                            {
                                Type = "integer",
                                Format = "bigint",
                            });
                            options.MapType<byte[]>(() => new OpenApiSchema()
                            {
                                Type = "string",
                                Format = "base64",
                            });
                            options.MapType<ReadOnlyMemory<byte>>(() => new OpenApiSchema()
                            {
                                Type = "string",
                                Format = "base64",
                            });
                            foreach (var plugin in Plugin.Plugins)
                            {
                                var assemblyName = plugin.GetType().Assembly.GetName().Name ?? nameof(RestServer);
                                var xmlPathAndFilename = Path.Combine(AppContext.BaseDirectory, "Plugins", assemblyName, $"{assemblyName}.xml");
                                if (File.Exists(xmlPathAndFilename))
                                    options.IncludeXmlComments(xmlPathAndFilename);
                            }
                        });
                        services.AddSwaggerGenNewtonsoftSupport();
                    }

                    #endregion

                    #region Forward Headers

                    if (_settings.EnableForwardedHeaders)
                        services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);

                    #endregion

                    #region Compression

                    if (_settings.EnableCompression)
                        services.Configure<GzipCompressionProviderOptions>(options => options.Level = _settings.CompressionLevel);

                    #endregion
                })
                .Configure(app =>
                {
                    app.UseMiddleware<RestServerMiddleware>();

                    if (_settings.EnableForwardedHeaders)
                        app.UseForwardedHeaders();

                    app.UseRouting();

                    if (_settings.EnableCors)
                        app.UseCors("All");

                    if (_settings.EnableCompression)
                        app.UseResponseCompression();

                    if (_settings.EnableBasicAuthentication)
                        app.UseAuthentication();

                    app.UseExceptionHandler(config =>
                        config.Run(async context =>
                        {
                            var exception = context.Features
                                .GetRequiredFeature<IExceptionHandlerPathFeature>()
                                .Error;
                            var response = new ErrorModel()
                            {
                                Code = exception.HResult,
                                Name = exception.GetType().Name,
                                Message = exception.InnerException?.Message ?? exception.Message,
                            };
                            RestServerMiddleware.SetServerInfomationHeader(context.Response);
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsJsonAsync(response);
                        }));

                    if (_settings.EnableSwagger)
                    {
                        app.UseSwagger();
                        //app.UseSwaggerUI(options => options.DefaultModelsExpandDepth(-1));
                        app.UseSwaggerUI(options =>
                        {
                            var apiVersionDescriptionProvider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
                            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                            {
                                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                            }
                        });
                    }

                    app.UseMvc();
                })
                .Build();
            _host.Start();
        }
    }
}
