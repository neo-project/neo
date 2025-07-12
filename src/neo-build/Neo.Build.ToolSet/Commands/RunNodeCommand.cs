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
using Neo.Build.ToolSet.Plugins;
using Neo.Persistence;
using Neo.Plugins.DBFTPlugin;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Commands
{
    internal class RunNodeCommand : Command
    {
        public RunNodeCommand() : base("run", CommandLineStrings.Node.RunDescription)
        {
            var walletPathOptions = new Option<string>(["--filename", "-f"], GetDefaultWalletFilename, "Wallet filename");
            var secondsPerBlockOptions = new Option<uint>(["--seconds-per-block", "-s"], GetDefaultSecondsPerBlock, "Seconds per blockchain block");
            //var traceOptions = new Option<bool>(["--enable-trace", "-t"], GetDefaultEnableTrace, "Enable VM tracing");

            AddOption(walletPathOptions);
            AddOption(secondsPerBlockOptions);
            //AddOption(traceOptions);
        }


        private static string GetDefaultWalletFilename() =>
            "wallet1.json";

        //private static bool GetDefaultEnableTrace() =>
        //    false;

        private static uint GetDefaultSecondsPerBlock() =>
            1000u;

        public new sealed class Handler(
            IHostEnvironment env,
            INeoConfigurationOptions neoConfiguration) : ICommandHandler
        {
            public required string Filename { get; set; }

            public bool EnableTrace { get; set; }

            public uint SecondsPerBlock { get; set; }

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

                var wallet = new DevWallet(walletModel, _neoConfiguration.ProtocolOptions.ToObject());
                var defaultMultiSigWalletAccount = wallet.GetMultiSigAccounts().SingleOrDefault() ??
                    // TODO: Create new exception class for this exception
                    throw new NeoBuildException("No Multi-Sig Address", NeoBuildErrorCodes.Wallet.AccountNotFoundException);

                using var mutex = FunctionFactory.CreateMutex(defaultMultiSigWalletAccount.Address);
                using var logPlugin = new LoggerPlugin(context.Console);
                using var dbftPlugin = new DBFTPlugin();
                var storeProvider = new FasterDbStoreProvider();

                StoreFactory.RegisterProvider(storeProvider);

                return Task.FromResult(0);
            }
        }
    }
}
