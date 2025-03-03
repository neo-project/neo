// Copyright (C) 2015-2025 The Neo Project.
//
// Settings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;
using Neo.Persistence.Providers;
using System;
using System.Reflection;
using System.Threading;

namespace Neo
{
    public class Settings
    {
        public LoggerSettings Logger { get; init; }
        public StorageSettings Storage { get; init; }
        public P2PSettings P2P { get; init; }
        public UnlockWalletSettings UnlockWallet { get; init; }
        public ContractsSettings Contracts { get; init; }
        public PluginsSettings Plugins { get; init; }

        static Settings? s_default;

        static bool UpdateDefault(IConfiguration configuration)
        {
            var settings = new Settings(configuration.GetSection("ApplicationConfiguration"));
            return null == Interlocked.CompareExchange(ref s_default, settings, null);
        }

        public static bool Initialize(IConfiguration configuration)
        {
            return UpdateDefault(configuration);
        }

        public static Settings Default
        {
            get
            {
                if (s_default == null)
                {
                    var config = new ConfigurationBuilder().AddJsonFile("config.json", optional: true).Build();
                    Initialize(config);
                }
                return Custom ?? s_default!;
            }
        }

        public static Settings? Custom { get; set; }

        public Settings(IConfigurationSection section)
        {
            Contracts = new(section.GetSection(nameof(Contracts)));
            Logger = new(section.GetSection(nameof(Logger)));
            Storage = new(section.GetSection(nameof(Storage)));
            P2P = new(section.GetSection(nameof(P2P)));
            UnlockWallet = new(section.GetSection(nameof(UnlockWallet)));
            Plugins = new(section.GetSection(nameof(Plugins)));
        }

        public Settings()
        {
            Logger = new LoggerSettings();
            Storage = new StorageSettings();
            P2P = new P2PSettings();
            UnlockWallet = new UnlockWalletSettings();
            Contracts = new ContractsSettings();
            Plugins = new PluginsSettings();
        }
    }

    public class LoggerSettings
    {
        public string Path { get; init; } = string.Empty;
        public bool ConsoleOutput { get; init; }
        public bool Active { get; init; }

        public LoggerSettings(IConfigurationSection section)
        {
            Path = section.GetValue(nameof(Path), "Logs")!;
            ConsoleOutput = section.GetValue(nameof(ConsoleOutput), false);
            Active = section.GetValue(nameof(Active), false);
        }

        public LoggerSettings() { }
    }

    public class StorageSettings
    {
        public string Engine { get; init; } = nameof(MemoryStore);
        public string Path { get; init; } = string.Empty;

        public StorageSettings(IConfigurationSection section)
        {
            Engine = section.GetValue(nameof(Engine), nameof(MemoryStore))!;
            Path = section.GetValue(nameof(Path), string.Empty)!;
        }

        public StorageSettings() { }
    }

    public class P2PSettings
    {
        public ushort Port { get; }
        public bool EnableCompression { get; }
        public int MinDesiredConnections { get; }
        public int MaxConnections { get; }
        public int MaxConnectionsPerAddress { get; }

        public P2PSettings(IConfigurationSection section)
        {
            Port = section.GetValue<ushort>(nameof(Port), 10333);
            EnableCompression = section.GetValue(nameof(EnableCompression), Peer.DefaultEnableCompression);
            MinDesiredConnections = section.GetValue(nameof(MinDesiredConnections), Peer.DefaultMinDesiredConnections);
            MaxConnections = section.GetValue(nameof(MaxConnections), Peer.DefaultMaxConnections);
            MaxConnectionsPerAddress = section.GetValue(nameof(MaxConnectionsPerAddress), 3);
        }

        public P2PSettings() { }
    }

    public class UnlockWalletSettings
    {
        public string? Path { get; init; } = string.Empty;
        public string? Password { get; init; } = string.Empty;
        public bool IsActive { get; init; } = false;

        public UnlockWalletSettings(IConfigurationSection section)
        {
            if (section.Exists())
            {
                Path = section.GetValue(nameof(Path), string.Empty)!;
                Password = section.GetValue(nameof(Password), string.Empty)!;
                IsActive = section.GetValue(nameof(IsActive), false);
            }
        }

        public UnlockWalletSettings() { }
    }

    public class ContractsSettings
    {
        public UInt160 NeoNameService { get; init; } = UInt160.Zero;

        public ContractsSettings(IConfigurationSection section)
        {
            if (section.Exists())
            {
                if (UInt160.TryParse(section.GetValue(nameof(NeoNameService), string.Empty), out var hash))
                {
                    NeoNameService = hash;
                }
                else
                    throw new ArgumentException("Neo Name Service (NNS): NeoNameService hash is invalid. Check your config.json.", nameof(NeoNameService));
            }
        }

        public ContractsSettings() { }
    }

    public class PluginsSettings
    {
        public Uri DownloadUrl { get; init; } = new("https://api.github.com/repos/neo-project/neo/releases");
        public bool Prerelease { get; init; } = false;
        public Version Version { get; init; } = Assembly.GetExecutingAssembly().GetName().Version!;

        public PluginsSettings(IConfigurationSection section)
        {
            if (section.Exists())
            {
                DownloadUrl = section.GetValue(nameof(DownloadUrl), DownloadUrl)!;
#if DEBUG
                Prerelease = section.GetValue(nameof(Prerelease), Prerelease);
                Version = section.GetValue(nameof(Version), Version)!;
#endif
            }
        }

        public PluginsSettings() { }
    }
}
