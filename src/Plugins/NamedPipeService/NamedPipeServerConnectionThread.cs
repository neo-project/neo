// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeServerConnectionThread.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Models;
using Neo.Plugins.Models.Payloads;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins
{
    internal sealed partial class NamedPipeServerConnectionThread(
        NeoSystem system,
        NamedPipeServerConnection connection) : IThreadPoolWorkItem, IAsyncDisposable
    {
        private readonly NeoSystem _system = system;
        private readonly NamedPipeServerConnection _connection = connection;

        private Exception? _shutdownException;

        public ValueTask DisposeAsync()
        {
            if (_shutdownException is not null)
                _connection.Abort(_shutdownException);
            return _connection.DisposeAsync();
        }

        public void Execute()
        {
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
                Utility.Log(nameof(NamedPipeServicePlugin), LogLevel.Error, "Connection timed out while writing to the client.");
            }
            catch (Exception ex)
            {
                _shutdownException = ex;
                Utility.Log(nameof(NamedPipeServicePlugin), LogLevel.Error, "Connection has stopped unexpectedly.");
            }
            finally
            {
                await DisposeAsync();
            }
        }

        private async Task OnRequestMessageAsync(PipeMessage message)
        {
            var responseMessage = message.Command switch
            {
                PipeCommand.GetBlockHeight => OnBlockHeight(message),
                PipeCommand.GetBlock => OnBlock(message),
                _ => CreateErrorResponse(message.RequestId, new InvalidDataException()),
            };

            await WriteAsync(responseMessage);
        }

        private async Task WriteAsync(PipeMessage message)
        {
            var memory = message.ToArray().AsMemory();

            _ = await _connection.Writer.WriteAsync(memory);
        }

        private PipeMessage CreateErrorResponse(int requestId, Exception exception)
        {
            var error = new PipeExceptionPayload(exception);
            return PipeMessage.Create(requestId, PipeCommand.Exception, error);
        }
    }
}
