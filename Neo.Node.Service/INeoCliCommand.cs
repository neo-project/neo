// Copyright (C) 2015-2024 The Neo Project.
//
// INeoCliCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Threading;
using System.Threading.Tasks;

namespace Neo.Node.Service
{
    internal enum CommandType : byte
    {
        None = 0x00,
        Exit = 0xee,
    }

    internal interface INeoCliCommand
    {
        CommandType Command { get; }
        string Name { get; }
        Task ExecuteAsync(CancellationToken cancellationToken);
    }

    internal class PipeCommand : INeoCliCommand
    {
        public CommandType Command { get; set; } = CommandType.None;
        public string Name { get; set; } = string.Empty;

        public Task ExecuteAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
