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
using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.Build.ToolSet.Providers
{
    internal sealed class NeoBuildConfigurationProvider : ConfigurationProvider
    {
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

            Data.Add(HostDefaults.EnvironmentKey, HostingEnvironments.Localnet);
            Data.Add(HostDefaults.ContentRootKey, Environment.CurrentDirectory);
            Data.Add(ConfigurationNames.HomeRootKey, ProgramDefaults.HomeRootPath);
            Data.Add(ConfigurationNames.CheckpointRootKey, ProgramDefaults.CheckpointRootPath);

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
            var normalizedPrefix = ConfigurationNames.PREFIX + ':';

            if (normalizedKey.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                data[normalizedKey[normalizedPrefix.Length..]] = value;
        }

        private static string Normalize(string key) =>
            key.Replace("_", ConfigurationPath.KeyDelimiter);
    }
}
