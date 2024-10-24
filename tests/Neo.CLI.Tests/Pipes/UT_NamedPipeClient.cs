// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NamedPipeClient.cs file belongs to the neo project and is free
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Tests.Pipes
{
    [TestClass]
    public class UT_NamedPipeClient
    {
        [TestMethod]
        public async Task Send_Echo_Message()
        {
            var endPoint = new NamedPipeEndPoint(Path.GetRandomFileName());
            await using var listener = new NamedPipeListener(endPoint);
            await using var client = new NamedPipeClient(endPoint);

            listener.Start();
            await client.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(1));

            await using var conn = await listener.AcceptAsync();
            await using var threadItem = new NamedPipeMessageProtocol(conn!);
            ThreadPool.UnsafeQueueUserWorkItem(threadItem, preferLocal: false);

            // double send same message
            var expectedEchoMessage = new NamedPipeMessage { RequestId = 666, Command = NamedPipeCommand.Echo, Payload = new EchoPayload { Message = "Hello World!" } };
            var actual1 = await client.SendMessageAsync(expectedEchoMessage).AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            var actual2 = await client.SendMessageAsync(expectedEchoMessage).AsTask().WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(expectedEchoMessage.RequestId, actual1.RequestId);
            Assert.AreEqual(expectedEchoMessage.Command, actual1.Command);

            Assert.AreEqual(expectedEchoMessage.RequestId, actual2.RequestId);
            Assert.AreEqual(expectedEchoMessage.Command, actual2.Command);
        }
    }
}
