// Copyright (C) 2015-2025 The Neo Project.
//
// Settings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neo.Plugins.RpcServer
{
    class Settings : PluginSettings
    {
        public IReadOnlyList<RpcServerSettings> Servers { get; init; }

        public Settings(IConfigurationSection section) : base(section)
        {
            Servers = section.GetSection(nameof(Servers)).GetChildren().Select(p => RpcServerSettings.Load(p)).ToArray();
        }
    }

    public record RpcServerSettings
    {
        public uint Network { get; init; }
        public IPAddress BindAddress { get; init; }
        public ushort Port { get; init; }
        public string SslCert { get; init; }
        public string SslCertPassword { get; init; }
        public string[] TrustedAuthorities { get; init; }
        public int MaxConcurrentConnections { get; init; }
        public int MaxRequestBodySize { get; init; }
        public string RpcUser { get; init; }
        public string RpcPass { get; init; }
        public bool EnableCors { get; init; }
        public string[] AllowOrigins { get; init; }
        public int KeepAliveTimeout { get; init; }
        public uint RequestHeadersTimeout { get; init; }
        // In the unit of datoshi, 1 GAS = 10^8 datoshi
        public long MaxGasInvoke { get; init; }
        // In the unit of datoshi, 1 GAS = 10^8 datoshi
        public long MaxFee { get; init; }
        public int MaxIteratorResultItems { get; init; }
        public int MaxStackSize { get; init; }
        public string[] DisabledMethods { get; init; }
        public bool SessionEnabled { get; init; }
        public TimeSpan SessionExpirationTime { get; init; }
        public int FindStoragePageSize { get; init; }

        public static RpcServerSettings Default { get; } = new RpcServerSettings
        {
            Network = 5195086u,
            BindAddress = IPAddress.None,
            SslCert = string.Empty,
            SslCertPassword = string.Empty,
            MaxGasInvoke = (long)new BigDecimal(10M, NativeContract.GAS.Decimals).Value,
            MaxFee = (long)new BigDecimal(0.1M, NativeContract.GAS.Decimals).Value,
            TrustedAuthorities = Array.Empty<string>(),
            EnableCors = true,
            AllowOrigins = Array.Empty<string>(),
            KeepAliveTimeout = 60,
            RequestHeadersTimeout = 15,
            MaxIteratorResultItems = 100,
            MaxStackSize = ushort.MaxValue,
            DisabledMethods = Array.Empty<string>(),
            MaxConcurrentConnections = 40,
            MaxRequestBodySize = 5 * 1024 * 1024,
            SessionEnabled = false,
            SessionExpirationTime = TimeSpan.FromSeconds(60),
            FindStoragePageSize = 50
        };

        public static RpcServerSettings Load(IConfigurationSection section) => new()
        {
            Network = section.GetValue("Network", Default.Network),
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value),
            Port = ushort.Parse(section.GetSection("Port").Value),
            SslCert = section.GetSection("SslCert").Value,
            SslCertPassword = section.GetSection("SslCertPassword").Value,
            TrustedAuthorities = section.GetSection("TrustedAuthorities").GetChildren().Select(p => p.Get<string>()).ToArray(),
            RpcUser = section.GetSection("RpcUser").Value,
            RpcPass = section.GetSection("RpcPass").Value,
            EnableCors = section.GetValue(nameof(EnableCors), Default.EnableCors),
            AllowOrigins = section.GetSection(nameof(AllowOrigins)).GetChildren().Select(p => p.Get<string>()).ToArray(),
            KeepAliveTimeout = section.GetValue(nameof(KeepAliveTimeout), Default.KeepAliveTimeout),
            RequestHeadersTimeout = section.GetValue(nameof(RequestHeadersTimeout), Default.RequestHeadersTimeout),
            MaxGasInvoke = (long)new BigDecimal(section.GetValue<decimal>("MaxGasInvoke", Default.MaxGasInvoke), NativeContract.GAS.Decimals).Value,
            MaxFee = (long)new BigDecimal(section.GetValue<decimal>("MaxFee", Default.MaxFee), NativeContract.GAS.Decimals).Value,
            MaxIteratorResultItems = section.GetValue("MaxIteratorResultItems", Default.MaxIteratorResultItems),
            MaxStackSize = section.GetValue("MaxStackSize", Default.MaxStackSize),
            DisabledMethods = section.GetSection("DisabledMethods").GetChildren().Select(p => p.Get<string>()).ToArray(),
            MaxConcurrentConnections = section.GetValue("MaxConcurrentConnections", Default.MaxConcurrentConnections),
            MaxRequestBodySize = section.GetValue("MaxRequestBodySize", Default.MaxRequestBodySize),
            SessionEnabled = section.GetValue("SessionEnabled", Default.SessionEnabled),
            SessionExpirationTime = TimeSpan.FromSeconds(section.GetValue("SessionExpirationTime", (int)Default.SessionExpirationTime.TotalSeconds)),
            FindStoragePageSize = section.GetValue("FindStoragePageSize", Default.FindStoragePageSize)
        };
    }
}
