// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildConfigurationProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Neo.Build.ToolSet.Configuration;
using Neo.Build.ToolSet.Services;
using Neo.Network.P2P;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Neo.Build.ToolSet.Providers
{
    internal sealed class NeoBuildConfigurationProvider : ConfigurationProvider
    {
        private const string PREFIX = "NEOBUILD";

        private readonly NeoBuildConfigurationSource _source;

        public NeoBuildConfigurationProvider(
            NeoBuildConfigurationSource source)
        {
            _source = source;

            if (_source.InitialData != null)
            {
                foreach (var pair in _source.InitialData)
                    Data.Add(pair.Key, pair.Value);
            }

            // Hosting Environment
            Data.Add(HostDefaults.EnvironmentKey, HostingEnvironments.Localnet);
            Data.Add(HostDefaults.ContentRootKey, Environment.CurrentDirectory);

            // Storage Environment
            Data.Add(ProgramDefaults.CheckpointRootKey, ProgramDefaults.CheckpointRootPath);

            // Node Network Configuration
            Data.Add(NeoSystemDefaults.ListenKey, $"{IPAddress.Loopback}");
            Data.Add(NeoSystemDefaults.PortKey, "0");
            Data.Add(NeoSystemDefaults.MinDesiredConnectionsKey, $"{Peer.DefaultMinDesiredConnections}");
            Data.Add(NeoSystemDefaults.MaxConnectionsKey, $"{Peer.DefaultMaxConnections}");
            Data.Add(NeoSystemDefaults.MaxConnectionsPerAddressKey, "3");
            Data.Add(NeoSystemDefaults.EnableCompressionKey, $"{Peer.DefaultEnableCompression}");

            // Other default configurations here

            Load(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));
            Load(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process));
        }

        private void Load(IDictionary envVariables)
        {
            var iter = envVariables.GetEnumerator();

            try
            {
                while (iter.MoveNext())
                {
                    var key = (string)iter.Entry.Key;
                    var value = (string?)iter.Entry.Value;

                    AddIfNormalizedKeyMatchesPrefix(Data, Normalize(key), value);
                }
            }
            finally
            {
                (iter as IDisposable)?.Dispose();
            }
        }

        private static void AddIfNormalizedKeyMatchesPrefix(IDictionary<string, string?> data, string normalizedKey, string? value)
        {
            var normalizedPrefix = PREFIX + ':';

            if (normalizedKey.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                data[normalizedKey[normalizedPrefix.Length..]] = value;
        }

        private static string Normalize(string key) =>
            key.Replace("_", ConfigurationPath.KeyDelimiter);
    }
}
