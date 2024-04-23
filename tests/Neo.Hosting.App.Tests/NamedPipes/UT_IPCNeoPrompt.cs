// Copyright (C) 2015-2024 The Neo Project.
//
// UT_IPCNeoPrompt.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.NamedPipes.Protocol;
using System.Text;

namespace Neo.Hosting.App.Tests.NamedPipes
{
    public class UT_IPCNeoPrompt
    {
        private static readonly byte[] s_testData = Encoding.UTF8.GetBytes("Hello world");

        [Fact]
        public async Task PipeVersion_ToBytesArray_ValidSerializeData()
        {
            var version = new PipeVersion();

            using var ms = new MemoryStream();
            await version.CopyToAsync(ms).DefaultTimeout();

            var newVersion = new PipeVersion();

            ms.Position = 0;
            await newVersion.CopyFromAsync(ms).DefaultTimeout();

            Assert.Equal(version.Version, newVersion.Version);
            Assert.Equal(version.Plugins, newVersion.Plugins);
            Assert.Equal(version.Platform, newVersion.Platform);
            Assert.Equal(version.TimeStamp, newVersion.TimeStamp);
            Assert.Equal(version.MachineName, newVersion.MachineName);
            Assert.Equal(version.UserName, newVersion.UserName);
            Assert.Equal(version.ProcessId, newVersion.ProcessId);
            Assert.Equal(version.ProcessPath, newVersion.ProcessPath);
            Assert.Equal(version.ToArray(), newVersion.ToArray());
        }

        [Fact]
        public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
        {
            await using var connectionListener = await NamedPipeTransportFactory.CreateConnectionListener();
            var clientConnection = NamedPipeTransportFactory.CreateClientStream(connectionListener.EndPoint);

            // Client connecting
            await clientConnection.ConnectAsync().DefaultTimeout();

            // Server accepting stream
            var serverConnectionTask = connectionListener.AcceptAsync();

            // Client sending data
            var writeTask = clientConnection.WriteAsync(s_testData);

            var serverConnection = await serverConnectionTask.DefaultTimeout();
            await writeTask.DefaultTimeout();

            // Server reading data
            var readResult = await serverConnection!.Transport!.Input.ReadAtLeastAsync(s_testData.Length).DefaultTimeout();
            serverConnection.Transport.Input.AdvanceTo(readResult.Buffer.End);

            clientConnection.Close();

            readResult = await serverConnection.Transport.Input.ReadAsync();
            Assert.True(readResult.IsCompleted);

            // Server completing input and output
            await serverConnection.Transport.Input.CompleteAsync();
            await serverConnection.Transport.Output.CompleteAsync();

            // Server disposing connection
            await serverConnection.DisposeAsync();
        }
    }
}
