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
using Neo.Build.Core.Factories;
using Neo.Build.ToolSet.Configuration;
using Neo.Network.P2P;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

            // Node Storage Configuration
            var protocolNetwork = FunctionFactory.GetDevNetwork(0);
            var storeRoot = Path.Combine(Environment.CurrentDirectory, $"Store_{protocolNetwork:X08}");
            var checkpointRoot = Path.Combine(Environment.CurrentDirectory, $"Checkpoints_{protocolNetwork:X08}");

            Data.Add(NeoSystemConfigurationNames.StoreRootKey, storeRoot);
            Data.Add(NeoSystemConfigurationNames.CheckpointRootKey, checkpointRoot);

            // Node Network Configuration
            Data.Add(NeoSystemConfigurationNames.ListenKey, $"{IPAddress.Loopback}");
            Data.Add(NeoSystemConfigurationNames.PortKey, $"{RandomFactory.NextUInt16()}");
            Data.Add(NeoSystemConfigurationNames.MinDesiredConnectionsKey, $"{Peer.DefaultMinDesiredConnections}");
            Data.Add(NeoSystemConfigurationNames.MaxConnectionsKey, $"{Peer.DefaultMaxConnections}");
            Data.Add(NeoSystemConfigurationNames.MaxConnectionsPerAddressKey, "3");
            Data.Add(NeoSystemConfigurationNames.EnableCompressionKey, $"{Peer.DefaultEnableCompression}");

            // Protocol Configuration
            Data.Add(ProtocolSettingsConfigurationNames.NetworkKey, $"{protocolNetwork}");
            Data.Add(ProtocolSettingsConfigurationNames.AddressVersionKey, "53");
            Data.Add(ProtocolSettingsConfigurationNames.MillisecondsPerBlockKey, "1000");
            Data.Add(ProtocolSettingsConfigurationNames.MaxTransactionsPerBlockKey, "512");
            Data.Add(ProtocolSettingsConfigurationNames.MemoryPoolMaxTransactionsKey, "50000");
            Data.Add(ProtocolSettingsConfigurationNames.MaxTraceableBlocksKey, "2102400");
            Data.Add(ProtocolSettingsConfigurationNames.InitialGasDistributionKey, "5200000000000000");

            // Application Engine Configuration
            Data.Add(AppEngineConfigurationNames.MaxGasKey, "2000000000");

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
