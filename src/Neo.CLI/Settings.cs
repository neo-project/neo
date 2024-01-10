// Copyright (C) 2015-2024 The Neo Project.
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
using System.Threading;

namespace Neo
{
    public class Settings
    {
        public LoggerSettings Logger { get; }
        public StorageSettings Storage { get; }
        public P2PSettings P2P { get; }
        public UnlockWalletSettings UnlockWallet { get; }

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

                return s_default!;
            }
        }

        public Settings(IConfigurationSection section)
        {
            Logger = new(section.GetSection(nameof(Logger)));
            Storage = new(section.GetSection(nameof(Storage)));
            P2P = new(section.GetSection(nameof(P2P)));
            UnlockWallet = new(section.GetSection(nameof(UnlockWallet)));
        }
    }

    public class LoggerSettings
    {
        public string Path { get; }
        public bool ConsoleOutput { get; }
        public bool Active { get; }

        public LoggerSettings(IConfigurationSection section)
        {
            Path = section.GetValue(nameof(Path), "Logs")!;
            ConsoleOutput = section.GetValue(nameof(ConsoleOutput), false);
            Active = section.GetValue(nameof(Active), false);
        }
    }

    public class StorageSettings
    {
        public string Engine { get; } = string.Empty;
        public string Path { get; } = string.Empty;

        public StorageSettings(IConfigurationSection section)
        {
            Engine = section.GetValue(nameof(Engine), "LevelDBStore")!;
            Path = section.GetValue(nameof(Path), "Data_LevelDB_{0}")!;
        }
    }

    public class P2PSettings
    {
        public ushort Port { get; }
        public int MinDesiredConnections { get; }
        public int MaxConnections { get; }
        public int MaxConnectionsPerAddress { get; }

        public P2PSettings(IConfigurationSection section)
        {
            Port = section.GetValue<ushort>(nameof(Port), 10333);
            MinDesiredConnections = section.GetValue(nameof(MinDesiredConnections), Peer.DefaultMinDesiredConnections);
            MaxConnections = section.GetValue(nameof(MaxConnections), Peer.DefaultMaxConnections);
            MaxConnectionsPerAddress = section.GetValue(nameof(MaxConnectionsPerAddress), 3);
        }
    }

    public class UnlockWalletSettings
    {
        public string Path { get; } = string.Empty;
        public string Password { get; } = string.Empty;
        public bool IsActive { get; } = false;

        public UnlockWalletSettings(IConfigurationSection section)
        {
            if (section.Exists())
            {
                Path = section.GetValue(nameof(Path), string.Empty)!;
                Password = section.GetValue(nameof(Password), string.Empty)!;
                IsActive = section.GetValue(nameof(IsActive), false);
            }
        }
    }
}
