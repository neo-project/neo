// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeMessageProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Pipes.Protocols
{
    internal class NamedPipeMessageProtocol(
        NamedPipeConnection namedPipeConnection) : IThreadPoolWorkItem, IAsyncDisposable
    {
        private readonly NamedPipeConnection _connection = namedPipeConnection;
        private readonly CancellationTokenSource _ctsMessagesReceived = new();

        public ValueTask DisposeAsync()
        {
            return _connection.DisposeAsync();
        }

        public void Execute()
        {
            _ = ProcessData();
        }

        private async Task ProcessData()
        {
            try
            {
                var tempts = 0;

                while (true)
                {
                    if (tempts + 1 >= 3)
                        break;

                    var reader = _connection.Transport.Input;
                    var result = await reader.ReadAsync();

                    if (result.IsCanceled)
                        break;

                    var buffer = result.Buffer;
                    var message = GetMessage(buffer.ToArray());

                    if (message is null)
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                        tempts++;
                    }
                    else
                    {
                        var messageSequence = buffer.Slice(0, message.Size);
                        reader.AdvanceTo(messageSequence.End);

                        _ = OnMessageReceivedAsync(message);

                        tempts = 0;
                    }

                    if (result.IsCompleted)
                        break;
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                await DisposeAsync();
            }
        }

        private async Task OnMessageReceivedAsync(NamedPipeMessage message)
        {
            var responseMessage = message.Command switch
            {
                NamedPipeCommand.Echo => message,
                _ => throw new InvalidOperationException(),
            };

            await SendMessage(responseMessage);
        }

        public static NamedPipeMessage? GetMessage(byte[] buffer)
        {
            try
            {
                return NamedPipeMessage.Deserialize(buffer);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task SendMessage(NamedPipeMessage message)
        {
            var writer = _connection.Transport.Output;
            var result = await writer.WriteAsync(message.ToByteArray());

            if (result.IsCompleted == false)
                throw new IOException("Failed to send message");
        }
    }
}
