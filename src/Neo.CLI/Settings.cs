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

        static Settings? _default;

        static bool UpdateDefault(IConfiguration configuration)
        {
            var settings = new Settings(configuration.GetSection("ApplicationConfiguration"));
            return null == Interlocked.CompareExchange(ref _default, settings, null);
        }

        public static bool Initialize(IConfiguration configuration)
        {
            return UpdateDefault(configuration);
        }

        public static Settings Default
        {
            get
            {
                if (_default == null)
                {
                    IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("config.json", optional: true).Build();
                    Initialize(config);
                }

                return _default!;
            }
        }

        public Settings(IConfigurationSection section)
        {
            Logger = new LoggerSettings(section.GetSection("Logger"));
            Storage = new StorageSettings(section.GetSection("Storage"));
            P2P = new P2PSettings(section.GetSection("P2P"));
            UnlockWallet = new UnlockWalletSettings(section.GetSection("UnlockWallet"));
        }
    }

    public class LoggerSettings
    {
        public string Path { get; }
        public bool ConsoleOutput { get; }
        public bool Active { get; }

        public LoggerSettings(IConfigurationSection section)
        {
            Path = section.GetValue("Path", "Logs")!;
            ConsoleOutput = section.GetValue("ConsoleOutput", false);
            Active = section.GetValue("Active", false);
        }
    }

    public class StorageSettings
    {
        public string Engine { get; }
        public string Path { get; }

        public StorageSettings(IConfigurationSection section)
        {
            Engine = section.GetValue("Engine", "LevelDBStore")!;
            Path = section.GetValue("Path", "Data_LevelDB_{0}")!;
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
            Port = section.GetValue<ushort>("Port", 10333);
            MinDesiredConnections = section.GetValue("MinDesiredConnections", Peer.DefaultMinDesiredConnections);
            MaxConnections = section.GetValue("MaxConnections", Peer.DefaultMaxConnections);
            MaxConnectionsPerAddress = section.GetValue("MaxConnectionsPerAddress", 3);
        }
    }

    public class UnlockWalletSettings
    {
        public string? Path { get; }
        public string? Password { get; }
        public bool IsActive { get; }

        public UnlockWalletSettings(IConfigurationSection section)
        {
            if (section.Exists())
            {
                Path = section.GetValue("Path", "");
                Password = section.GetValue("Password", "");
                IsActive = bool.Parse(section.GetValue("IsActive", "false")!);
            }
        }
    }
}
