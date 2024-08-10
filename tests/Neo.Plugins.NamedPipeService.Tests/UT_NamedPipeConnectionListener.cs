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

using Microsoft.Testing.Platform.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Plugins.Models;
using System.Threading.Tasks;

namespace Neo.Plugins.NamedPipeService.Tests
{
    [TestClass]
    public class UT_NamedPipeConnectionListener
    {
        private static readonly IPipeMessage s_testPipeMessage = PipeMessage.Create(1, PipeCommand.NAck, PipeMessage.Null);

        [TestMethod]
        public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName());
            var clientConnection = NamedPipeFactory.CreateClientStream(connectionListener.LocalEndPoint);

            // Server startup
            connectionListener.Start();

            // Client connecting
            await clientConnection.ConnectAsync().DefaultTimeout();

            // Server accepting stream
            var serverConnectionTask = connectionListener.AcceptAsync();

            // Client sending data
            var bytes = s_testPipeMessage.ToArray();
            var writeTask = clientConnection.WriteAsync(bytes);

            var serverConnection = await serverConnectionTask.DefaultTimeout();
            await writeTask.DefaultTimeout();

            // Server reading data
            var readResult = await serverConnection.ReadAsync().DefaultTimeout();
            Assert.IsNotNull(readResult);

            clientConnection.Close();

            var countResult = serverConnection.MessageQueueCount;
            Assert.AreEqual(0, countResult);

            // Server disposing connection
            await serverConnection.DisposeAsync();
        }
    }
}
