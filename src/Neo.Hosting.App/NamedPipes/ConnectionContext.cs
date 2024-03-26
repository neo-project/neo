// Copyright (C) 2015-2024 The Neo Project.
//
// ConnectionContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes
{
    internal abstract class ConnectionContext : IAsyncDisposable
    {
        public abstract IDuplexPipe? Transport { get; set; }

        public abstract void Abort(Exception abortReason);

        public void Abort() =>
            Abort(new OperationCanceledException());

        #region IAsyncDisposable

        public abstract ValueTask DisposeAsync();

        #endregion
    }
}
