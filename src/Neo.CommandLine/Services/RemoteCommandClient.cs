// Copyright (C) 2015-2024 The Neo Project.
//
// RemoteCommandClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CommandLine.Exceptions;
using Neo.CommandLine.Extensions;
using Neo.CommandLine.Services.Messages;
using Neo.CommandLine.Services.Payloads;
using Neo.CommandLine.Utilities;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CommandLine.Services
{
    internal sealed class RemoteCommandClient
    {
        public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        private readonly static string s_pipeName = $"neo.node\\{Program.ApplicationVersion}\\CommandPipe";

        private readonly NamedPipeClientStream _hostStream;

        public RemoteCommandClient(string serverName)
        {
            _hostStream = new NamedPipeClientStream(
                serverName, s_pipeName, PipeDirection.InOut,
                PipeOptions.CurrentUserOnly | PipeOptions.WriteThrough | PipeOptions.Asynchronous);
        }

        public RemoteCommandClient() : this(".")
        {

        }

        public Task<PipeVersionPayload?> GetVersionAsync(CancellationToken cancellationToken = default) =>
            _hostStream.TryCatchHandle(async () =>
            {
                await WaitForConnectionAsync(cancellationToken);

                _hostStream.Write(PipeMessage.Create(PipeCommand.Version));
                return _hostStream.ReadMessage() switch
                {
                    { Command: PipeCommand.Response, Payload: PipeVersionPayload version } => version,
                    { Command: PipeCommand.Error, Payload: ExceptionPayload error } => throw new HostServiceException(error),
                    _ => null
                };
            }, cancellationToken);

        public Task<bool> CreateBackupAsync(uint startBlockIndex = uint.MinValue, uint endBlockIndex = uint.MaxValue, CancellationToken cancellationToken = default) =>
            _hostStream.TryCatchHandle(async () =>
            {
                await WaitForConnectionAsync(cancellationToken);

                _hostStream.Write(PipeMessage.Create(PipeCommand.Backup, BackupPayload.Create(startBlockIndex, endBlockIndex)));
                return _hostStream.ReadMessage() switch
                {
                    { Command: PipeCommand.Response, Payload: BooleanPayload started } => started.Value,
                    { Command: PipeCommand.Error, Payload: ExceptionPayload error } => throw new HostServiceException(error),
                    _ => false
                };
            }, cancellationToken);

        private async Task WaitForConnectionAsync(CancellationToken cancellationToken = default)
        {
            var timeoutLinkedTokenSource = TaskUtilities.CreateTimeoutToken(Timeout, cancellationToken);
            await _hostStream.ConnectAsync(timeoutLinkedTokenSource.Token);
            if (_hostStream.IsConnected == false) throw new HostServiceDisconnectException();
        }
    }
}
