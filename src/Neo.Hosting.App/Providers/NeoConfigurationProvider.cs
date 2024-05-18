// Copyright (C) 2015-2024 The Neo Project.
//
// NeoConfigurationProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Util.Internal;
using Microsoft.Extensions.Configuration;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.Host;
using Neo.Network.P2P;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.Hosting.App.Providers
{
    internal class NeoConfigurationProvider
        (IConfigurationSection? configurationSection = null) : ConfigurationProvider
    {
        public override void Load()
        {
            Data = CreateDefaultKeys();
            Load(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));
            Load(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process));

            if (configurationSection is not null)
                Load(configurationSection);
        }

        internal void Load(IDictionary envVariables)
        {
            var e = envVariables.GetEnumerator();

            try
            {
                while (e.MoveNext())
                {
                    var key = (string)e.Entry.Key;
                    var value = (string?)e.Entry.Value;

                    AddIfNormalizedKeyMatchesPrefix(Data, Normalize(key), value);
                }
            }
            finally
            {
                (e as IDisposable)?.Dispose();
            }
        }

        internal void Load(IConfigurationSection section)
        {
            var e = section.AsEnumerable().GetEnumerator();
            var prefix = $"{section.Key}:";

            try
            {
                while (e.MoveNext())
                {
                    var key = e.Current.Key;
                    var value = e.Current.Value;

                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        Data.AddOrSet(key[prefix.Length..], value);
                }
            }
            finally
            {
                (e as IDisposable)?.Dispose();
            }
        }

        private static Dictionary<string, string?> CreateDefaultKeys() =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Storage
                ["STORAGE:PATH"] = "Data_LevelDB_{0:X2}",
                ["STORAGE:ENGINE"] = "LevelDBStore",
                ["STORAGE:VERIFY"] = bool.TrueString,
                ["STORAGE:ARCHIVE:PATH"] = AppContext.BaseDirectory,
                ["STORAGE:ARCHIVE:FILENAME"] = "chain.{0}.acc",

                // P2P
                ["P2P:LISTEN"] = "0.0.0.0",
                ["P2P:PORT"] = "10333",
                ["P2P:MINDESIREDCONNECTIONS"] = $"{Peer.DefaultMinDesiredConnections}",
                ["P2P:MAXCONNECTIONS"] = $"{Peer.DefaultMaxConnections}",
                ["P2P:MAXCONNECTIONSPERADDRESS"] = "3",

                // Contracts
                ["CONTRACT:NEONAMESERVICE"] = "0x50ac1c37690cc2cfc594472833cf57505d5f46de",

                // Plugin
                ["PLUGIN:DOWNLOADURL"] = "https://api.github.com/repos/neo-project/neo/releases",
                ["PLUGIN:PRERELEASE"] = bool.FalseString,
                ["PLUGIN:VERSION"] = $"{Program.ApplicationVersion.ToString(3)}",

                // Remote
                ["REMOTE:PIPENAME"] = NamedPipeServerFactory.GetUniquePipeName(),
                ["REMOTE:MAXPIPES"] = "16",
            };

        private static void AddIfNormalizedKeyMatchesPrefix(IDictionary<string, string?> data, string normalizedKey, string? value)
        {
            var normalizedPrefix1 = NeoEnvironmentVariableDefaults.PREFIX;
            var normalizedPrefix2 = $"NEO:";

            if (normalizedKey.StartsWith(normalizedPrefix1, StringComparison.OrdinalIgnoreCase))
                data[normalizedKey[normalizedPrefix1.Length..]] = value;
            else if (normalizedKey.StartsWith(normalizedPrefix2, StringComparison.OrdinalIgnoreCase))
                data[normalizedKey[normalizedPrefix2.Length..]] = value;
        }

        private static string Normalize(string key) =>
            key.Replace("__", ConfigurationPath.KeyDelimiter);
    }
}
