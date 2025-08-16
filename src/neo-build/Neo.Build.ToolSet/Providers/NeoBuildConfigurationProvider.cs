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
using Neo.Extensions.Factories;
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
            Data.Add(HostDefaults.EnvironmentKey, NeoHostingEnvironments.LocalNet);
            Data.Add(HostDefaults.ContentRootKey, Environment.CurrentDirectory);

            // Node Storage Configuration
            var protocolNetwork = FunctionFactory.GetDevNetwork(0);
            var storeRoot = Path.Combine(Environment.CurrentDirectory, $"Store_{protocolNetwork:X08}");
            var checkpointRoot = Path.Combine(Environment.CurrentDirectory, $"Checkpoints_{protocolNetwork:X08}");

            Data.Add(NeoSystemConfigurationNames.StoreRootKey, storeRoot);
            Data.Add(NeoSystemConfigurationNames.CheckpointRootKey, checkpointRoot);

            // Node Network Configuration
            Data.Add(NeoSystemConfigurationNames.ListenKey, $"{IPAddress.Loopback}");
            Data.Add(NeoSystemConfigurationNames.PortKey, $"{RandomNumberFactory.NextUInt16()}");
            Data.Add(NeoSystemConfigurationNames.MinDesiredConnectionsKey, "10");
            Data.Add(NeoSystemConfigurationNames.MaxConnectionsKey, "40");
            Data.Add(NeoSystemConfigurationNames.MaxConnectionsPerAddressKey, "3");
            Data.Add(NeoSystemConfigurationNames.EnableCompressionKey, bool.FalseString);

            // Protocol Configuration
            Data.Add(ProtocolOptionsConfigurationNames.NetworkKey, $"{protocolNetwork}");
            Data.Add(ProtocolOptionsConfigurationNames.AddressVersionKey, "53");
            Data.Add(ProtocolOptionsConfigurationNames.MillisecondsPerBlockKey, "1000");
            Data.Add(ProtocolOptionsConfigurationNames.MaxTransactionsPerBlockKey, "512");
            Data.Add(ProtocolOptionsConfigurationNames.MemoryPoolMaxTransactionsKey, "50000");
            Data.Add(ProtocolOptionsConfigurationNames.MaxTraceableBlocksKey, "2102400");
            Data.Add(ProtocolOptionsConfigurationNames.InitialGasDistributionKey, "5200000000000000");

            // Application Engine Configuration
            Data.Add(ApplicationEngineConfigurationNames.MaxGasKey, "2000000000");

            // DBFT Plugin Configuration
            var dbftRoot = Path.Combine(Environment.CurrentDirectory, $"DBFT_{protocolNetwork:X08}");

            Data.Add(DBFTConfigurationNames.RecoveryLogsStoreKey, dbftRoot);
            Data.Add(DBFTConfigurationNames.IgnoreRecoveryLogsKey, bool.TrueString);
            Data.Add(DBFTConfigurationNames.MaxBlockSizeKey, "2097152");
            Data.Add(DBFTConfigurationNames.MaxBlockSystemFeeKey, "150000000000");
            Data.Add(DBFTConfigurationNames.ExceptionPolicyKey, "StopNode");

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
