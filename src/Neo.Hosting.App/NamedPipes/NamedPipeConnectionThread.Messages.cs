// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeConnectionThread.Messages.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes
{
    internal partial class NamedPipeConnectionThread
    {
        private PipeMessage CreateErrorResponse(int requestId, Exception exception)
        {
            var error = new PipeException(exception);
            return PipeMessage.Create(requestId, PipeCommand.Exception, error);
        }

        private async Task OnRequestMessageAsync(PipeMessage message)
        {
            var responseMessage = message.Command switch
            {
                PipeCommand.GetVersion => OnVersion(message),
                PipeCommand.GetBlock => OnBlock(message),
                _ => CreateErrorResponse(message.RequestId, new InvalidDataException()),
            };

            await WriteAsync(responseMessage);
        }
        private PipeMessage OnVersion(PipeMessage message) =>
            PipeMessage.Create(message.RequestId, PipeCommand.Version, new PipeVersion());

        private PipeMessage OnBlock(PipeMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
