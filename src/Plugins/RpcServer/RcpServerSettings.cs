// Copyright (C) 2015-2025 The Neo Project.
//
// RcpServerSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
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
    class RpcServerSettings : IPluginSettings
    {
        public IReadOnlyList<RpcServersSettings> Servers { get; init; }

        public UnhandledExceptionPolicy ExceptionPolicy { get; }

        public RpcServerSettings(IConfigurationSection section)
        {
            Servers = [.. section.GetSection(nameof(Servers)).GetChildren().Select(RpcServersSettings.Load)];
            ExceptionPolicy = section.GetValue("UnhandledExceptionPolicy", UnhandledExceptionPolicy.Ignore);
        }
    }

    public record RpcServersSettings
    {
        public uint Network { get; init; } = 5195086u;
        public IPAddress BindAddress { get; init; } = IPAddress.Loopback;
        public ushort Port { get; init; } = 10332;
        public string SslCert { get; init; } = string.Empty;
        public string SslCertPassword { get; init; } = string.Empty;
        public string[] TrustedAuthorities { get; init; } = [];
        public int MaxConcurrentConnections { get; init; } = 40;
        public int MaxRequestBodySize { get; init; } = 5 * 1024 * 1024;
        public string RpcUser { get; init; } = string.Empty;
        public string RpcPass { get; init; } = string.Empty;
        public bool EnableCors { get; init; } = true;
        public string[] AllowOrigins { get; init; } = [];

        /// <summary>
        /// The maximum time in seconds allowed for the keep-alive connection to be idle.
        /// </summary>
        public int KeepAliveTimeout { get; init; } = 60;

        /// <summary>
        /// The maximum time in seconds allowed for the request headers to be read.
        /// </summary>
        public uint RequestHeadersTimeout { get; init; } = 15;

        /// <summary>
        /// In the unit of datoshi, 1 GAS = 10^8 datoshi
        /// </summary>
        public long MaxGasInvoke { get; init; } = (long)new BigDecimal(10M, NativeContract.GAS.Decimals).Value;

        /// <summary>
        /// In the unit of datoshi, 1 GAS = 10^8 datoshi
        /// </summary>
        public long MaxFee { get; init; } = (long)new BigDecimal(0.1M, NativeContract.GAS.Decimals).Value;
        public int MaxIteratorResultItems { get; init; } = 100;
        public int MaxStackSize { get; init; } = ushort.MaxValue;
        public string[] DisabledMethods { get; init; } = [];
        public bool SessionEnabled { get; init; } = false;
        public TimeSpan SessionExpirationTime { get; init; } = TimeSpan.FromSeconds(60);
        public int FindStoragePageSize { get; init; } = 50;

        public static RpcServersSettings Default { get; } = new();

        public static RpcServersSettings Load(IConfigurationSection section)
        {
            var @default = Default;
            return new()
            {
                Network = section.GetValue("Network", @default.Network),
                BindAddress = IPAddress.Parse(section.GetValue("BindAddress", @default.BindAddress.ToString())),
                Port = section.GetValue("Port", @default.Port),
                SslCert = section.GetValue("SslCert", string.Empty),
                SslCertPassword = section.GetValue("SslCertPassword", string.Empty),
                TrustedAuthorities = GetStrings(section, "TrustedAuthorities"),
                RpcUser = section.GetValue("RpcUser", @default.RpcUser),
                RpcPass = section.GetValue("RpcPass", @default.RpcPass),
                EnableCors = section.GetValue(nameof(EnableCors), @default.EnableCors),
                AllowOrigins = GetStrings(section, "AllowOrigins"),
                KeepAliveTimeout = section.GetValue(nameof(KeepAliveTimeout), @default.KeepAliveTimeout),
                RequestHeadersTimeout = section.GetValue(nameof(RequestHeadersTimeout), @default.RequestHeadersTimeout),
                MaxGasInvoke = (long)new BigDecimal(section.GetValue<decimal>("MaxGasInvoke", @default.MaxGasInvoke), NativeContract.GAS.Decimals).Value,
                MaxFee = (long)new BigDecimal(section.GetValue<decimal>("MaxFee", @default.MaxFee), NativeContract.GAS.Decimals).Value,
                MaxIteratorResultItems = section.GetValue("MaxIteratorResultItems", @default.MaxIteratorResultItems),
                MaxStackSize = section.GetValue("MaxStackSize", @default.MaxStackSize),
                DisabledMethods = GetStrings(section, "DisabledMethods"),
                MaxConcurrentConnections = section.GetValue("MaxConcurrentConnections", @default.MaxConcurrentConnections),
                MaxRequestBodySize = section.GetValue("MaxRequestBodySize", @default.MaxRequestBodySize),
                SessionEnabled = section.GetValue("SessionEnabled", @default.SessionEnabled),
                SessionExpirationTime = TimeSpan.FromSeconds(section.GetValue("SessionExpirationTime", (long)@default.SessionExpirationTime.TotalSeconds)),
                FindStoragePageSize = section.GetValue("FindStoragePageSize", @default.FindStoragePageSize)
            };
        }

        private static string[] GetStrings(IConfigurationSection section, string key)
        {
            List<string> list = [];
            foreach (var child in section.GetSection(key).GetChildren())
            {
                var value = child.Get<string>();
                if (value is null) throw new ArgumentException($"Invalid value for {key}");
                list.Add(value);
            }
            return list.ToArray();
        }
    }
}
