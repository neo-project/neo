// Copyright (C) 2016-2023 The Neo Project.
//
// The neo-cli is free software distributed under the MIT software
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Neo.CLI
{
    public partial class MainService
    {
        public int OnStartWithCommandLine(string[] args)
        {
            RootCommand rootCommand = new(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()!.Title)
            {
                new Option<string>(new[] { "-c", "--config","/config" }, "Specifies the config file."),
                new Option<string>(new[] { "-n", "--network","/network" }, "Indicates the network of the chain [mainnet, testnet, privnet]."),
                new Option<string>(new[] { "-w", "--wallet","/wallet" }, "The path of the neo3 wallet [*.json]."),
                new Option<string>(new[] { "-p", "--password" ,"/password" }, "Password to decrypt the wallet, either from the command line or config file."),
                new Option<string>(new[] { "--db-engine","/db-engine" }, "Specify the db engine."),
                new Option<string>(new[] { "--db-path","/db-path" }, "Specify the db path."),
                new Option<string>(new[] { "--noverify","/noverify" }, "Indicates whether the blocks need to be verified when importing."),
                new Option<string[]>(new[] { "--plugins","/plugins" }, "The path of the plugin [plugin1 plugin2]."),
            };

            rootCommand.Handler = CommandHandler.Create<RootCommand, CommandLineOptions, InvocationContext>(Handle);
            return rootCommand.Invoke(args);
        }

        private void Handle(RootCommand command, CommandLineOptions options, InvocationContext context)
        {
            Start(options);
        }

        private static void CustomProtocolSettings(CommandLineOptions options, ProtocolSettings settings)
        {
            uint network = settings.Network;
            ProtocolSettings tempSetting = settings;
            // if specified network without specifying config, then load the default config
            if (!string.IsNullOrEmpty(options.Network))
            {
                if (!uint.TryParse(options.Network, out network))
                {
                    network = options.Network switch
                    {
                        "mainnet" => 860833102,
                        "testnet" => 894710606,
                        _ => throw new Exception("Invalid network")
                    };
                }

                // if also specified config, then load the config and check the network
                if (!string.IsNullOrEmpty(options.Config))
                {
                    tempSetting = ProtocolSettings.Load(options.Config);
                    // the network in config file must match the network from command line
                    if (network != tempSetting.Network) throw new ArgumentException($"Network mismatch {network} {tempSetting.Network}");
                }
                else // if the network if specified without config, then load the default config
                {
                    tempSetting = ProtocolSettings.Load(network == 860833102 ? "config.mainnet.json" : "config.testnet.json");
                }
            }

            ProtocolSettings customSetting = new ProtocolSettings
            {
                Network = network,
                AddressVersion = tempSetting.AddressVersion,
                StandbyCommittee = tempSetting.StandbyCommittee,
                ValidatorsCount = tempSetting.ValidatorsCount,
                SeedList = tempSetting.SeedList,
                MillisecondsPerBlock = tempSetting.MillisecondsPerBlock,
                MaxTransactionsPerBlock = tempSetting.MaxTransactionsPerBlock,
                MemoryPoolMaxTransactions = tempSetting.MemoryPoolMaxTransactions,
                MaxTraceableBlocks = tempSetting.MaxTraceableBlocks,
                InitialGasDistribution = tempSetting.InitialGasDistribution,
                Hardforks = tempSetting.Hardforks
            };

            if (!string.IsNullOrEmpty(options.Config) || !string.IsNullOrEmpty(options.Network)) ProtocolSettings.Custom = customSetting;
        }

        private static void CustomApplicationSettings(CommandLineOptions options, Settings settings)
        {
            Settings tempSetting = string.IsNullOrEmpty(options.Config) ? settings : new Settings(new ConfigurationBuilder().AddJsonFile(options.Config, optional: true).Build().GetSection("ApplicationConfiguration"));

            Settings customSetting = new Settings
            {
                Logger = tempSetting.Logger,
                Storage = new StorageSettings
                {
                    Engine = options.DBEngine ?? tempSetting.Storage.Engine,
                    Path = options.DBPath ?? tempSetting.Storage.Path
                },
                P2P = tempSetting.P2P,
                UnlockWallet = new UnlockWalletSettings
                {
                    Path = options.Wallet ?? tempSetting.UnlockWallet.Path,
                    Password = options.Password ?? tempSetting.UnlockWallet.Password
                }
            };
            if (!string.IsNullOrEmpty(options.Config)
                || !string.IsNullOrEmpty(options.Network)
                || !string.IsNullOrEmpty(options.DBEngine)
                || !string.IsNullOrEmpty(options.DBPath)
                || !string.IsNullOrEmpty(options.Wallet)
                || !string.IsNullOrEmpty(options.Password)) Settings.Custom = customSetting;
        }
    }
}
