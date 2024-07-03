// Copyright (C) 2015-2024 The Neo Project.
//
// MainService.CommandLine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;

namespace Neo.CLI
{
    public partial class MainService
    {
        public int OnStartWithCommandLine(string[] args)
        {
            RootCommand rootCommand = new(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()!.Title)
            {
                new Option<string>(["-c", "--config","/config"], "Specifies the config file."),
                new Option<string>(["-w", "--wallet","/wallet"], "The path of the neo3 wallet [*.json]."),
                new Option<string>(["-p", "--password" ,"/password"], "Password to decrypt the wallet, either from the command line or config file."),
                new Option<string>(["--db-engine","/db-engine"], "Specify the db engine."),
                new Option<string>(["--db-path","/db-path"], "Specify the db path."),
                new Option<string>(["--noverify","/noverify"], "Indicates whether the blocks need to be verified when importing."),
                new Option<string[]>(["--plugins","/plugins"], "The list of plugins, if not present, will be installed [plugin1 plugin2]."),
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
            var tempSetting = settings;
            // if specified config, then load the config and check the network
            if (!string.IsNullOrEmpty(options.Config))
            {
                tempSetting = ProtocolSettings.Load(options.Config);
            }

            var customSetting = new ProtocolSettings
            {
                Network = tempSetting.Network,
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

            if (!string.IsNullOrEmpty(options.Config)) ProtocolSettings.Custom = customSetting;
        }

        private static void CustomApplicationSettings(CommandLineOptions options, Settings settings)
        {
            var tempSetting = string.IsNullOrEmpty(options.Config) ? settings : new Settings(new ConfigurationBuilder().AddJsonFile(options.Config, optional: true).Build().GetSection("ApplicationConfiguration"));
            var customSetting = new Settings
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
                },
                Contracts = tempSetting.Contracts
            };
            if (options.IsValid) Settings.Custom = customSetting;
        }
    }
}
