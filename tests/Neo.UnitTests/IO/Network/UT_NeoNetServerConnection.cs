// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoNetServerConnection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Network;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.UnitTests.Extensions;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Neo.UnitTests.IO.Network
{
    [TestClass]
    public class UT_NeoNetServerConnection
    {
        [TestMethod]
        public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
        {
            var expectedMessage = Message.Create(MessageCommand.Ping, new PingPayload { Nonce = 12345 });

            // Setup server and client connections
            var endPoint = new IPEndPoint(IPAddress.Loopback, 1);
            using var tcpListener = new TcpListener(endPoint);
            using var clientConnection = new TcpClient();

            // Server startup
            tcpListener.Start();

            // Client connecting
            await clientConnection.ConnectAsync(endPoint).DefaultTimeout();

            // Server accepting stream
            var serverSocket = await tcpListener.AcceptSocketAsync().DefaultTimeout();
            await using var connection = new NeoNetConnection(endPoint, serverSocket, PipeOptions.Default, PipeOptions.Default);
            connection.Start();

            // Client sending data
            var bytes = expectedMessage.ToArray(false);
            var clientStream = clientConnection.GetStream();
            await clientStream.WriteAsync(bytes).DefaultTimeout();

            // connection reading data
            var actualMessage = await connection.ReceiveAsync().DefaultTimeout();
            Assert.IsNotNull(actualMessage);
            Assert.AreEqual(expectedMessage.Command, actualMessage.Command);
            Assert.IsInstanceOfType<PingPayload>(actualMessage.Payload);

            // Client disconnecting
            clientConnection.Close();

            // connection disposing
            await connection.DisposeAsync();

            // server cleanup
            tcpListener.Stop();
        }
    }
}
