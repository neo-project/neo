// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeConnectionThread.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes
{
    internal partial class NamedPipeConnectionThread(
        NamedPipeServerConnection connection,
        ILoggerFactory loggerFactory) : IThreadPoolWorkItem, IAsyncDisposable
    {
        internal const int MaxTimeoutSeconds = 15;

        private readonly NamedPipeServerConnection _connection = connection;
        private readonly ILogger _logger = loggerFactory.CreateLogger<NamedPipeConnectionThread>();

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(MaxTimeoutSeconds);

        private Exception? _shutdownException;

        public ValueTask DisposeAsync()
        {
            if (_shutdownException is not null)
                _connection.Abort(_shutdownException);
            return _connection.DisposeAsync();
        }

        public void Execute()
        {
            _logger.LogDebug("Connection has started.");

            _ = ProcessRequests();
        }

        private async Task ProcessRequests()
        {
            try
            {
                PipeMessage? message;

                while ((message = await _connection.ReadAsync()) != null)
                {
                    await OnRequestMessageAsync(message);
                }

            }
            catch (TimeoutException ex)
            {
                _shutdownException = ex;
                _logger.LogError(ex, "Connection timed out while writing to the client.");
            }
            catch (Exception ex)
            {
                _shutdownException = ex;
                _logger.LogError(ex, "Connection has stopped unexpectedly.");
            }
            finally
            {
                await DisposeAsync();
            }
        }

        public async Task WriteAsync(PipeMessage message)
        {
            var memory = message.ToArray().AsMemory();

            _ = await _connection.Writer.WriteAsync(memory).TimeoutAfter(_timeout);
        }
    }
}
