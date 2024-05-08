// Copyright (C) 2015-2024 The Neo Project.
//
// IPCConnection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Hosting.App.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal sealed class IPCConnection : IThreadPoolWorkItem
    {

        private readonly NamedPipeConnection _transportConnection;

        private readonly ILogger _logger;

        public IPCConnection(
            NamedPipeConnection transportConnection,
            ILogger logger)
        {
            _transportConnection = transportConnection;
            _logger = logger;
        }

        void IThreadPoolWorkItem.Execute()
        {
            _ = StartProtocolAsync();
        }

        internal async Task StartProtocolAsync()
        {
            try
            {
                // TODO: Implement the IPC protocol
                var result = await _transportConnection.Application.Input.ReadPipeMessage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IPC connection error");
            }
            finally
            {
                await _transportConnection.DisposeAsync();
            }
        }
    }
}
