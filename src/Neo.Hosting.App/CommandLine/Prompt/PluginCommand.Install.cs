// Copyright (C) 2015-2024 The Neo Project.
//
// PluginCommand.Install.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal partial class PluginCommand
    {
        internal sealed class InstallCommand : Command
        {
            public InstallCommand() : base("install", "Install plugins")
            {
                var pluginNameArgument = new Argument<string>("PLUGIN_NAME", "Name of plugin");

                this.SetHandler(async context => await new Handler().InvokeAsync(context));
            }

            internal sealed new class Handler : ICommandHandler
            {
                public Task<int> InvokeAsync(InvocationContext context)
                {
                    return Task.FromResult(0);
                }

                public int Invoke(InvocationContext context)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
