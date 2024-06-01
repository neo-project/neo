// Copyright (C) 2015-2024 The Neo Project.
//
// ExportCommand.Blocks.cs file belongs to the neo project and is free
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
using System.IO;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal partial class ExportCommand
    {
        internal sealed class BlocksCommand : Command
        {
            public BlocksCommand() : base("blocks", "Export blocks from on-chain.")
            {
                var startIndexArgument = new Argument<uint>("START", "Starting height index of the block.");
                var countOption = new Option<uint>(["--count", "-c"], () => uint.MaxValue, "Total blocks to be written from <START>.");
                var fileOption = new Option<FileInfo>(["--file", "-f"], "Output filename");

                AddArgument(startIndexArgument);
                AddOption(countOption);
                AddOption(fileOption);

                this.SetHandler(async context => await new Handler().InvokeAsync(context));
            }
        }

        public new sealed class Handler : ICommandHandler
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
