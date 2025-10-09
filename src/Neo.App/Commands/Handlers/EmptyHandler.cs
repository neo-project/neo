// Copyright (C) 2015-2025 The Neo Project.
//
// EmptyHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.App.Commands.Handlers
{
    internal sealed class EmptyHandler : ICommandHandler
    {
        public int Invoke(InvocationContext context) =>
            InvokeAsync(context).GetAwaiter().GetResult();

        public Task<int> InvokeAsync(InvocationContext context) =>
            Task.FromResult(0);
    }
}
