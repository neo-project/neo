// Copyright (C) 2015-2025 The Neo Project.
//
// RestServerSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Plugins.RestServer.Newtonsoft.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.IO.Compression;
using System.Net;

namespace Neo.Plugins.RestServer
{
    public class RestServerSettings
    {
        #region Settings

        public uint Network { get; init; }
        public IPAddress BindAddress { get; init; } = IPAddress.None;
        public uint Port { get; init; }
        public uint KeepAliveTimeout { get; init; }
        public string? SslCertFile { get; init; }
        public string? SslCertPassword { get; init; }
        public string[] TrustedAuthorities { get; init; } = [];
        public bool EnableBasicAuthentication { get; init; }
        public string RestUser { get; init; } = string.Empty;
        public string RestPass { get; init; } = string.Empty;
        public bool EnableCors { get; init; }
        public string[] AllowOrigins { get; init; } = [];
        public string[] DisableControllers { get; init; } = [];
        public bool EnableCompression { get; init; }
        public CompressionLevel CompressionLevel { get; init; }
        public bool EnableForwardedHeaders { get; init; }
        public bool EnableSwagger { get; init; }
        public uint MaxPageSize { get; init; }
        public long MaxConcurrentConnections { get; init; }
        public long MaxGasInvoke { get; init; }
        public required JsonSerializerSettings JsonSerializerSettings { get; init; }

        #endregion

        #region Static Functions

        public static RestServerSettings Default { get; } = new()
        {
            Network = 860833102u,
            BindAddress = IPAddress.Loopback,
            Port = 10339u,
            KeepAliveTimeout = 120u,
            SslCertFile = "",
            SslCertPassword = "",
            TrustedAuthorities = Array.Empty<string>(),
            EnableBasicAuthentication = false,
            RestUser = string.Empty,
            RestPass = string.Empty,
            EnableCors = false,
            AllowOrigins = Array.Empty<string>(),
            DisableControllers = Array.Empty<string>(),
            EnableCompression = false,
            CompressionLevel = CompressionLevel.SmallestSize,
            EnableForwardedHeaders = false,
            EnableSwagger = false,
            MaxPageSize = 50u,
            MaxConcurrentConnections = 40L,
            MaxGasInvoke = 0_200000000L,
            JsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.None,
                Converters =
                [
                    new StringEnumConverter(),
                    new BigDecimalJsonConverter(),
                    new BlockHeaderJsonConverter(),
                    new BlockJsonConverter(),
                    new ContractAbiJsonConverter(),
                    new ContractEventDescriptorJsonConverter(),
                    new ContractGroupJsonConverter(),
                    new ContractInvokeParametersJsonConverter(),
                    new ContractJsonConverter(),
                    new ContractManifestJsonConverter(),
                    new ContractMethodJsonConverter(),
                    new ContractMethodParametersJsonConverter(),
                    new ContractParameterDefinitionJsonConverter(),
                    new ContractParameterJsonConverter(),
                    new ContractPermissionDescriptorJsonConverter(),
                    new ContractPermissionJsonConverter(),
                    new ECPointJsonConverter(),
                    new GuidJsonConverter(),
                    new InteropInterfaceJsonConverter(),
                    new MethodTokenJsonConverter(),
                    new NefFileJsonConverter(),
                    new ReadOnlyMemoryBytesJsonConverter(),
                    new SignerJsonConverter(),
                    new StackItemJsonConverter(),
                    new TransactionAttributeJsonConverter(),
                    new TransactionJsonConverter(),
                    new UInt160JsonConverter(),
                    new UInt256JsonConverter(),
                    new VmArrayJsonConverter(),
                    new VmBooleanJsonConverter(),
                    new VmBufferJsonConverter(),
                    new VmByteStringJsonConverter(),
                    new VmIntegerJsonConverter(),
                    new VmMapJsonConverter(),
                    new VmNullJsonConverter(),
                    new VmPointerJsonConverter(),
                    new VmStructJsonConverter(),
                    new WitnessConditionJsonConverter(),
                    new WitnessJsonConverter(),
                    new WitnessRuleJsonConverter(),
                ],
            },
        };

        public static RestServerSettings Current { get; private set; } = Default;

        public static void Load(IConfigurationSection section) =>
            Current = new()
            {
                Network = section.GetValue(nameof(Network), Default.Network),
                BindAddress = IPAddress.Parse(section.GetSection(nameof(BindAddress)).Value ?? "0.0.0.0"),
                Port = section.GetValue(nameof(Port), Default.Port),
                KeepAliveTimeout = section.GetValue(nameof(KeepAliveTimeout), Default.KeepAliveTimeout),
                SslCertFile = section.GetValue(nameof(SslCertFile), Default.SslCertFile),
                SslCertPassword = section.GetValue(nameof(SslCertPassword), Default.SslCertPassword),
                TrustedAuthorities = section.GetSection(nameof(TrustedAuthorities))?.Get<string[]>() ?? Default.TrustedAuthorities,
                EnableBasicAuthentication = section.GetValue(nameof(EnableBasicAuthentication), Default.EnableBasicAuthentication),
                RestUser = section.GetValue(nameof(RestUser), Default.RestUser) ?? string.Empty,
                RestPass = section.GetValue(nameof(RestPass), Default.RestPass) ?? string.Empty,
                EnableCors = section.GetValue(nameof(EnableCors), Default.EnableCors),
                AllowOrigins = section.GetSection(nameof(AllowOrigins))?.Get<string[]>() ?? Default.AllowOrigins,
                DisableControllers = section.GetSection(nameof(DisableControllers))?.Get<string[]>() ?? Default.DisableControllers,
                EnableCompression = section.GetValue(nameof(EnableCompression), Default.EnableCompression),
                CompressionLevel = section.GetValue(nameof(CompressionLevel), Default.CompressionLevel),
                EnableForwardedHeaders = section.GetValue(nameof(EnableForwardedHeaders), Default.EnableForwardedHeaders),
                EnableSwagger = section.GetValue(nameof(EnableSwagger), Default.EnableSwagger),
                MaxPageSize = section.GetValue(nameof(MaxPageSize), Default.MaxPageSize),
                MaxConcurrentConnections = section.GetValue(nameof(MaxConcurrentConnections), Default.MaxConcurrentConnections),
                MaxGasInvoke = section.GetValue(nameof(MaxGasInvoke), Default.MaxGasInvoke),
                JsonSerializerSettings = Default.JsonSerializerSettings,
            };

        #endregion
    }
}
