// Copyright (C) 2015-2024 The Neo Project.
//
// UnitTest1.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI.Pipes;
using Neo.CLI.Pipes.Protocols;
using Neo.CLI.Pipes.Protocols.Payloads;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Tests
{
    [TestClass]
    public class UT_NamedPipeListener
    {
        private static readonly NamedPipeEndPoint _endPoint = new(Path.GetRandomFileName());

        [TestMethod]
        public async Task ServerEcho_Message()
        {
            await using var listener = new NamedPipeListener(_endPoint);
            await using var client = new NamedPipeClientStream(_endPoint.ServerName, _endPoint.PipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous | PipeOptions.WriteThrough);

            listener.Start();
            await client.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(1));

            await using var conn = await listener.AcceptAsync();
            await using var threadItem = new NamedPipeMessageProtocol(conn!);
            ThreadPool.UnsafeQueueUserWorkItem(threadItem, preferLocal: false);

            var echoMessage = new EchoPayload { Message = "Hello World!" };
            var expectedMessage = new NamedPipeMessage { Command = NamedPipeCommand.Echo, Payload = echoMessage };

            await client.WriteAsync(expectedMessage.ToByteArray()).AsTask().WaitAsync(TimeSpan.FromSeconds(1));

            var buffer = new byte[expectedMessage.Size];
            var count = client.Read(buffer, 0, buffer.Length);
            var actualMessage = NamedPipeMessage.Deserialize(buffer);

            Assert.AreEqual(expectedMessage.Command, actualMessage.Command);
            Assert.AreEqual(expectedMessage.Size, actualMessage.Size);
            Assert.AreEqual(expectedMessage.PayloadSize, actualMessage.PayloadSize);
            Assert.AreEqual(((EchoPayload)expectedMessage.Payload!).Message, ((EchoPayload)actualMessage.Payload!).Message);
        }
    }
}
