// Copyright (C) 2015-2025 The Neo Project.
//
// NeoConfigurationProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Neo.App.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.App.Providers
{
    internal class NeoConfigurationProvider : ConfigurationProvider
    {
        private const string PREFIX = "NEO";

        private readonly NeoConfigurationSource _source;

        public NeoConfigurationProvider(
            NeoConfigurationSource source)
        {
            _source = source;

            if (_source.InitialData != null)
            {
                foreach (var pair in _source.InitialData)
                    Data.Add(pair.Key, pair.Value);
            }

            // Hosting Environment
            Data.Add(HostDefaults.EnvironmentKey, NeoHostingEnvironments.MainNet);
            Data.Add(HostDefaults.ContentRootKey, AppContext.BaseDirectory);

            // Node Storage Configuration
            Data.Add(ApplicationConfigurationNames.StoreEngineKey, "LevelDbStore");
            Data.Add(ApplicationConfigurationNames.StorePathKey, "Data_LevelDB_{0}");

            // Node Network Configuration
            Data.Add(ApplicationConfigurationNames.ListenKey, "0.0.0.0");
            Data.Add(ApplicationConfigurationNames.PortKey, "10333");
            Data.Add(ApplicationConfigurationNames.EnableCompressionKey, bool.TrueString);
            Data.Add(ApplicationConfigurationNames.MinDesiredConnectionsKey, "10");
            Data.Add(ApplicationConfigurationNames.MaxConnectionsKey, "40");
            Data.Add(ApplicationConfigurationNames.MaxKnownHashesKey, "1000");
            Data.Add(ApplicationConfigurationNames.MaxConnectionsPerAddressKey, "3");

            // Protocol Configuration
            Data.Add(ProtocolConfigurationNames.NetworkKey, "860833102");
            Data.Add(ProtocolConfigurationNames.AddressVersionKey, "53");
            Data.Add(ProtocolConfigurationNames.MillisecondsPerBlockKey, "15000");
            Data.Add(ProtocolConfigurationNames.MaxTransactionsPerBlockKey, "512");
            Data.Add(ProtocolConfigurationNames.MemoryPoolMaxTransactionsKey, "50000");
            Data.Add(ProtocolConfigurationNames.MaxTraceableBlocksKey, "2102400");
            Data.Add(ProtocolConfigurationNames.InitialGasDistributionKey, "5200000000000000");

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
