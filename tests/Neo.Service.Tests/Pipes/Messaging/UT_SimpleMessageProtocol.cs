// Copyright (C) 2015-2024 The Neo Project.
//
// UT_SimpleMessageProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging.Abstractions;
using Neo.IO.Buffers;
using Neo.IO.Pipes;
using Neo.IO.Pipes.Protocols;
using Neo.IO.Pipes.Protocols.Payloads;
using Neo.Persistence;
using Neo.Service.Pipes;
using Neo.Service.Pipes.Messaging;
using Neo.Service.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Service.Tests.Pipes.Messaging
{
    [TestClass]
    public class UT_SimpleMessageProtocol
    {
        [TestMethod]
        public async Task TestEchoMessage()
        {
            using var neoSystem = new NeoSystem(TestProtocolSettings.Default, nameof(MemoryStore));
            var endPoint = new NamedPipeEndPoint(Path.GetRandomFileName());
            await using var listener = new NamedPipeListener(endPoint, NullLogger<NamedPipeListener>.Instance);
            await using var client = new NamedPipeClientStream(endPoint.ServerName, endPoint.PipeName);

            listener.Start();
            await client.ConnectAsync();

            // Accept client and get connection
            await using var conn = await listener.AcceptAsync();
            await using var protocol = new SimpleMessageProtocol(conn, neoSystem, new(), NullLogger<SimpleMessageProtocol>.Instance);
            ThreadPool.UnsafeQueueUserWorkItem(protocol, preferLocal: false);

            var expectedMessage = new NamedPipeMessage()
            {
                Command = NamedPipeCommand.Echo,
                Payload = new EchoPayload()
                {
                    Message = "Hello World!",
                },
            };

            client.Write(expectedMessage.ToByteArray());

            var actualResult = NamedPipeMessage.TryDeserialize(client, out var actualMessage);

            Assert.IsTrue(actualResult);
            Assert.AreEqual(((EchoPayload)expectedMessage.Payload).Message, ((EchoPayload)actualMessage.Payload).Message);
        }

        [TestMethod]
        public async Task TestServerInfoMessage()
        {
            using var neoSystem = new NeoSystem(TestProtocolSettings.Default, nameof(MemoryStore));
            var endPoint = new NamedPipeEndPoint(Path.GetRandomFileName());
            await using var listener = new NamedPipeListener(endPoint, NullLogger<NamedPipeListener>.Instance);
            await using var client = new NamedPipeClientStream(endPoint.ServerName, endPoint.PipeName);

            listener.Start();
            await client.ConnectAsync();

            // Accept client and get connection
            await using var conn = await listener.AcceptAsync();
            await using var protocol = new SimpleMessageProtocol(conn, neoSystem, new(), NullLogger<SimpleMessageProtocol>.Instance);
            ThreadPool.UnsafeQueueUserWorkItem(protocol, preferLocal: false);

            var expectedMessage = new NamedPipeMessage()
            {
                Command = NamedPipeCommand.ServerInfo,
                Payload = new EmptyPayload(),
            };

            client.Write(expectedMessage.ToByteArray());

            var actualResult = NamedPipeMessage.TryDeserialize(client, out var actualMessage);

            Assert.IsTrue(actualResult);
            Assert.AreEqual(IPAddress.Any, ((ServerInfoPayload)actualMessage.Payload).Address);
            Assert.AreEqual(10333, ((ServerInfoPayload)actualMessage.Payload).Port);
        }
    }
}
