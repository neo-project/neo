// Copyright (C) 2015-2025 The Neo Project.
//
// RunNodeCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Neo.Build.Core;
using Neo.Build.Core.Exceptions;
using Neo.Build.Core.Extensions;
using Neo.Build.Core.Factories;
using Neo.Build.Core.Models;
using Neo.Build.Core.Models.Wallets;
using Neo.Build.Core.Providers.Storage;
using Neo.Build.Core.Wallets;
using Neo.Build.ToolSet.Configuration;
using Neo.Build.ToolSet.Options;
using Neo.Build.ToolSet.Plugins;
using Neo.Build.ToolSet.Providers;
using Neo.Persistence;
using Neo.Plugins.DBFTPlugin;
using Neo.SmartContract;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Commands
{
    internal class RunNodeCommand : Command
    {
        public RunNodeCommand() : base("run", "Run Neo instance node")
        {
            var walletPathOptions = new Option<string>(["--filename", "-f"], GetDefaultWalletFilename, "Wallet filename");
            var secondsPerBlockOptions = new Option<uint>(["--seconds-per-block", "-s"], GetDefaultSecondsPerBlock, "Seconds per blockchain block");
            //var traceOptions = new Option<bool>(["--enable-trace", "-t"], GetDefaultEnableTrace, "Enable VM tracing");

            AddOption(walletPathOptions);
            AddOption(secondsPerBlockOptions);
            //AddOption(traceOptions);
        }


        private static string GetDefaultWalletFilename() =>
            FileNameDefaults.WalletName;

        //private static bool GetDefaultEnableTrace() =>
        //    false;

        private static uint GetDefaultSecondsPerBlock() =>
            1u;

        public new sealed class Handler(
            IHostEnvironment env,
            INeoConfigurationOptions neoConfiguration) : ICommandHandler
        {
            public string Filename { get; set; } = GetDefaultWalletFilename();

            public bool EnableTrace { get; set; }

            public uint SecondsPerBlock { get; set; } = GetDefaultSecondsPerBlock();

            private readonly IHostEnvironment _env = env;
            private readonly INeoConfigurationOptions _neoConfiguration = neoConfiguration;

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).ConfigureAwait(false).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                var walletFileInfo = FunctionFactory.ResolveFileName(Filename, _env.ContentRootPath);
                if (walletFileInfo.Exists == false)
                    throw new NeoBuildFileNotFoundException(walletFileInfo.FullName);

                var walletModel = JsonModel.FromJson<WalletModel>(walletFileInfo) ??
                    throw new NeoBuildInvalidFileFormatException(walletFileInfo.FullName);

                var globalProtocolOptions = _neoConfiguration.ProtocolOptions.ToObject();
                var wallet = new DevWallet(walletModel);
                var defaultMultiSigWalletAccount = wallet.GetMultiSigAccounts().FirstOrDefault() ??
                    wallet.GetDefaultAccount() ??
                    // TODO: Create new exception class for this exception
                    throw new NeoBuildException("No Multi-Sig Address", NeoBuildErrorCodes.Wallet.AccountNotFound);

                var storeProvider = new FasterDbStoreProvider(_neoConfiguration.StorageOptions.CheckPointRoot);
                StoreFactory.RegisterProvider(storeProvider);

                var dbftSettings = GetConsensusSettings(_neoConfiguration.DBFTOptions, wallet.ProtocolSettings);

                using var mutex = FunctionFactory.CreateMutex($"{_neoConfiguration.NetworkOptions.Listen}_{_neoConfiguration.NetworkOptions.Port}");
                using var logPlugin = new LoggerPlugin(context.Console);
                using var dbftPlugin = new DBFTPlugin(dbftSettings);
                using var neoSystem = new NeoSystem(wallet.ProtocolSettings with { MillisecondsPerBlock = SecondsPerBlock * 1000 }, storeProvider, _neoConfiguration.StorageOptions.StoreRoot);

                ApplicationEngine.Provider = TraceApplicationEngineProvider.Instance;

                neoSystem.StartNode(new()
                {
                    Tcp = new(_neoConfiguration.NetworkOptions.Listen, _neoConfiguration.NetworkOptions.Port)
                });
                dbftPlugin.Start(wallet);

                var cts = context.GetCancellationToken();

                while (cts.IsCancellationRequested == false)
                    Thread.Sleep(1000);

                return Task.FromResult(0);
            }

            private static DbftSettings GetConsensusSettings(DBFTOptions dBFTOptions, ProtocolSettings protocolSettings)
            {
                var settings = new Dictionary<string, string>()
                {
                    { "PluginConfiguration:Network", $"{protocolSettings.Network}" },
                    { "PluginConfiguration:RecoveryLogs", $"{dBFTOptions.StoreRoot}" },
                    { "PluginConfiguration:IgnoreRecoveryLogs", $"{dBFTOptions.IgnoreRecoveryLogs}" },
                    { "PluginConfiguration:MaxBlockSize", $"{dBFTOptions.MaxBlockSize}" },
                    { "PluginConfiguration:MaxBlockSystemFee", $"{dBFTOptions.MaxBlockSystemFee}" },
                    { "PluginConfiguration:UnhandledExceptionPolicy", $"{dBFTOptions.ExceptionPolicy}" },
                    { "PluginConfiguration:AutoStart", bool.FalseString },
                };

                var config = new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
                return new DbftSettings(config.GetSection("PluginConfiguration"));
            }
        }
    }
}
