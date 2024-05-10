// Copyright (C) 2015-2024 The Neo Project.
//
// IPCServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal sealed class IPCServer(
        NamedPipeConnection transportConnection,
        ILogger logger) : IThreadPoolWorkItem
    {

        private readonly NamedPipeConnection _transportConnection = transportConnection;

        private readonly ILogger _logger = logger;

        void IThreadPoolWorkItem.Execute()
        {
            _ = ProcessMessagesAsync();
        }

        internal async Task ProcessMessagesAsync()
        {
            var reader = _transportConnection.Transport.Input;

            try
            {
                while (_transportConnection.ConnectionClosed == false)
                {
                    var readResult = await reader.ReadAsync();

                    if (readResult.IsCanceled || readResult.IsCompleted)
                        break;

                    var buffer = readResult.Buffer;
                    PipeMessage? message = null;
                    try
                    {
                        message = PipeMessage.Create(buffer.First);
                    }
                    catch
                    {
                        // Invalid or corrupt message
                    }
                    finally
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                        _ = ProcessMessageAsync(message);
                    }
                }
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

        internal async Task ProcessMessageAsync(PipeMessage? message)
        {
            var writer = _transportConnection.Transport.Output;

            var result = await writer.WriteAsync(new PipeMessage().ToArray().AsMemory());

        }
    }
}
