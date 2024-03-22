// Copyright (C) 2015-2024 The Neo Project.
//
// ArchiveCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Native;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Service.App.Commands
{
    internal sealed class ArchiveCommand : Command
    {
        public ArchiveCommand() : base("archive", "File archive management")
        {
            AddOption(new Option<uint>(new[] { "--start", "-s" }, () => 1, "Block index to begin archive"));
            AddOption(new Option<uint>(new[] { "--count", "-c" }, () => uint.MaxValue, "Number of blocks to archive"));
        }

        public new sealed class Handler : ICommandHandler
        {
            public uint Start { get; set; }
            public uint Count { get; set; }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var host = context.GetHost();
                var neoSystem = NeoSystemService.Instance ?? throw new NullReferenceException("NeoSystem");
                var currentBlockHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

                Count = Math.Min(Count, currentBlockHeight - Start);

                return 0;
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
