// Copyright (C) 2016-2023 The Neo Project.
//
// The neo-cli is free software distributed under the MIT software
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php
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
        public LoggerSettings Logger { get; init; }
        public StorageSettings Storage { get; init; }
        public P2PSettings P2P { get; init; }
        public UnlockWalletSettings UnlockWallet { get; init; }

        static Settings _default;

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

                return Custom ?? _default;
            }
        }

        public static Settings? Custom { get; set; }

        public Settings(IConfigurationSection section)
        {
            this.Logger = new LoggerSettings(section.GetSection("Logger"));
            this.Storage = new StorageSettings(section.GetSection("Storage"));
            this.P2P = new P2PSettings(section.GetSection("P2P"));
            this.UnlockWallet = new UnlockWalletSettings(section.GetSection("UnlockWallet"));
        }

        public Settings()
        {
        }
    }

    public class LoggerSettings
    {
        public string Path { get; init; }
        public bool ConsoleOutput { get; init; }
        public bool Active { get; init; }

        public LoggerSettings(IConfigurationSection section)
        {
            this.Path = section.GetValue("Path", "Logs");
            this.ConsoleOutput = section.GetValue("ConsoleOutput", false);
            this.Active = section.GetValue("Active", false);
        }
    }

    public class StorageSettings
    {
        public string Engine { get; init; }
        public string Path { get; init; }

        public StorageSettings(IConfigurationSection section)
        {
            this.Engine = section.GetValue("Engine", "LevelDBStore");
            this.Path = section.GetValue("Path", "Data_LevelDB_{0}");
        }

        public StorageSettings()
        {
        }
    }

    public class P2PSettings
    {
        public ushort Port { get; init; }
        public ushort WsPort { get; init; }
        public int MinDesiredConnections { get; init; }
        public int MaxConnections { get; init; }
        public int MaxConnectionsPerAddress { get; init; }

        public P2PSettings(IConfigurationSection section)
        {
            this.Port = ushort.Parse(section.GetValue("Port", "10333"));
            this.WsPort = ushort.Parse(section.GetValue("WsPort", "10334"));
            this.MinDesiredConnections = section.GetValue("MinDesiredConnections", Peer.DefaultMinDesiredConnections);
            this.MaxConnections = section.GetValue("MaxConnections", Peer.DefaultMaxConnections);
            this.MaxConnectionsPerAddress = section.GetValue("MaxConnectionsPerAddress", 3);
        }
    }

    public class UnlockWalletSettings
    {
        public string Path { get; init; }
        public string Password { get; init; }
        public bool IsActive { get; init; }

        public UnlockWalletSettings(IConfigurationSection section)
        {
            if (section.Exists())
            {
                this.Path = section.GetValue("Path", "");
                this.Password = section.GetValue("Password", "");
                this.IsActive = bool.Parse(section.GetValue("IsActive", "false"));
            }
        }

        public UnlockWalletSettings()
        {
        }
    }
}
