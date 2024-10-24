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

using Neo.CLI.Hosting.Services;
using Neo.CLI.Pipes.Protocols.Payloads;
using Neo.Extensions;
using Neo.SmartContract.Native;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
            try
            {
                var responseMessage = message.Command switch
                {
                    NamedPipeCommand.Echo => message,
                    NamedPipeCommand.ServerInfo => OnServerInfo(message),
                    _ => throw new InvalidOperationException(),
                };

                await SendMessage(responseMessage);
            }
            catch
            {
                // Send Error Message to Client
            }
        }

        private NamedPipeMessage OnServerInfo(NamedPipeMessage message)
        {
            var neoSystem = NeoSystemHostedService.NeoSystem ?? throw new InvalidOperationException("NeoSystem is not set");
            var options = NeoSystemHostedService.Options ?? throw new InvalidOperationException("Options is not set");
            var localNode = NeoSystemHostedService.LocalNode ?? throw new InvalidOperationException("LocalNode is not set");
            var height = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);
            var responsePayload = new ServerInfoPayload
            {
                Nonce = (uint)Random.Shared.Next(),
                Version = (uint)Assembly.GetExecutingAssembly().GetVersionNumber(),
                Address = IPAddress.Parse(options.P2P.Listen),
                Port = options.P2P.Port,
                BlockHeight = height,
                HeaderHeight = neoSystem.HeaderCache.Last?.Index ?? height,

                RemoteNodes = [.. localNode.GetRemoteNodes().Select(s =>
                    new ServerInfoPayload.RemoteConnectedClient
                    {
                        Address = s.Remote.Address,
                        Port = (ushort)s.Remote.Port,
                        LastBlockIndex = s.LastBlockIndex,
                    })],
            };

            return new NamedPipeMessage() { RequestId = message.RequestId, Command = NamedPipeCommand.ServerInfo, Payload = responsePayload, };
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
