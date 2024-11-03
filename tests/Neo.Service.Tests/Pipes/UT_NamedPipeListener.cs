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

using Microsoft.Extensions.Logging.Abstractions;
using Neo;
using Neo.IO.Buffers;
using Neo.IO.Pipes;
using Neo.Persistence;
using Neo.Service;
using Neo.Service.Pipes;
using Neo.Service.Pipes.Messaging;
using Neo.Service.Tests;
using Neo.Service.Tests.Helpers;
using Neo.Service.Tests.Pipes;
using System.Buffers;
using System.IO.Pipes;
using System.Text;

namespace Neo.Service.Tests.Pipes
{
    [TestClass]
    public class UT_NamedPipeListener
    {
        [TestMethod]
        public async Task TestWriteFromClientAndReadFromServer()
        {
            var endPoint = new NamedPipeEndPoint(Path.GetRandomFileName());
            await using var listener = new NamedPipeListener(endPoint, NullLogger<NamedPipeListener>.Instance);
            await using var client = new NamedPipeClientStream(endPoint.ServerName, endPoint.PipeName);

            listener.Start();
            await client.ConnectAsync();

            // Accept client and get connection
            await using var conn = await listener.AcceptAsync();

            // Write data to the server from the client
            byte[] expectedBytes = [0x01, 0x02, 0x03, 0x04, 0x05];
            client.WriteAsync(expectedBytes);

            // Read the sent data from client
            var actualResult = await conn.Transport.Input.ReadAsync();

            Assert.IsFalse(actualResult.IsCompleted);
            Assert.IsFalse(actualResult.IsCanceled);
            CollectionAssert.AreEqual(expectedBytes, actualResult.Buffer.ToArray());
        }

        [TestMethod]
        public async Task TestConsoleMessageProtocol()
        {
            using var neoSystem = new NeoSystem(TestProtocolSettings.Default, nameof(MemoryStore));
            var endPoint = new NamedPipeEndPoint(Path.GetRandomFileName());
            await using var listener = new NamedPipeListener(endPoint, NullLogger<NamedPipeListener>.Instance);
            await using var client = new NamedPipeClientStream(endPoint.ServerName, endPoint.PipeName);

            listener.Start();
            await client.ConnectAsync();

            var sw = new StreamWriter(client) { AutoFlush = true, };
            var sr = new StreamReader(client);

            // Accept client and get connection
            await using var conn = await listener.AcceptAsync();
            await using var thread = new ConsoleMessageProtocol(conn, neoSystem, NullLogger.Instance);
            ThreadPool.UnsafeQueueUserWorkItem(thread, false);

            // Write data to the server
            sw.WriteLine("help");

            // Read response from the server
            var line = sr.ReadLine();

            Assert.IsNotNull(line);
        }
    }
}
