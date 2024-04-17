// Copyright (C) 2015-2024 The Neo Project.
//
// DummyHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Hosting.App.Handlers
{
    public sealed class EmptyHandler : ICommandHandler
    {
        public int Invoke(InvocationContext context)
        {
            return 0;
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            return Task.FromResult(0);
        }
    }
}
