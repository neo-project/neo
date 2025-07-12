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
using Neo.Build.ToolSet.Configuration;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Commands
{
    internal class RunNodeCommand : Command
    {
        public RunNodeCommand() : base("run", CommandLineStrings.Node.RunDescription)
        {
            var walletPathOptions = new Option<string>(["--filename", "-f"], GetDefaultWalletFilename, "Wallet filename");
            var traceOptions = new Option<bool>(["--enable-trace", "-t"], GetDefaultEnableTrace, "Enable VM tracing");
            var secondsPerBlockOptions = new Option<uint>(["--seconds-per-block", "-s"], GetDefaultSecondsPerBlock, "Seconds per blockchain block");

            AddOption(walletPathOptions);
            AddOption(secondsPerBlockOptions);
            AddOption(traceOptions);
        }


        private static string GetDefaultWalletFilename() =>
            "wallet1.json";

        private static bool GetDefaultEnableTrace() =>
            false;

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
                return Task.FromResult(0);
            }
        }
    }
}
