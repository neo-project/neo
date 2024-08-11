// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NamedPipeServerConnectionThread.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Plugins.Models;
using Neo.Plugins.Models.Payloads;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.NamedPipeService.Tests
{
    [TestClass]
    public class UT_NamedPipeServerConnectionThread
    {
        private static readonly NeoSystem s_neoSystem = new NeoSystem(TestDefaults.DefaultProtocolSettings, new TestDefaults.StoreProvider());

        private NamedPipeServerListener _connectionListener;
        private NamedPipeClientStream _clientConnection;

        [TestInitialize]
        public async Task InitTest()
        {
            _connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName());
            _clientConnection = NamedPipeFactory.CreateClientStream(_connectionListener.LocalEndPoint);

            // Server startup
            _connectionListener.Start();

            // Client connecting
            await _clientConnection.ConnectAsync().DefaultTimeout();

            // Server accepting stream
            var serverConnectionTask = await _connectionListener.AcceptAsync().DefaultTimeout();

            var threadPoolItem = new NamedPipeServerConnectionThread(s_neoSystem, serverConnectionTask);

            ThreadPool.UnsafeQueueUserWorkItem(threadPoolItem, preferLocal: false);
        }

        [TestMethod]
        public async Task SendAndReceive_Messages_GetBlockHeight()
        {
            var rid = Random.Shared.Next();
            var getBlockHeightPayload = PipeMessage.Create(rid, PipeCommand.GetBlockHeight, PipeMessage.Null);

            var writeTask = _clientConnection.WriteAsync(getBlockHeightPayload.ToArray());

            var buffer = new byte[1024];
            var count = await _clientConnection.ReadAsync(buffer).DefaultTimeout();

            var message = PipeMessage.Create(buffer);

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.BlockHeight, message.Command);
            Assert.AreEqual(rid, message.RequestId);
        }

        [TestMethod]
        public async Task SendAndReceive_Messages_GetBlock()
        {
            var rid = Random.Shared.Next();

            var getBlockPayload = PipeMessage.Create(rid, PipeCommand.GetBlock, new PipeUnmanagedPayload<uint>()
            {
                Value = 0,
            });

            var writeTask = _clientConnection.WriteAsync(getBlockPayload.ToArray());

            var buffer = new byte[1024];
            var count = await _clientConnection.ReadAsync(buffer).DefaultTimeout();

            var message = PipeMessage.Create(buffer);

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.Block, message.Command);
            Assert.AreEqual(rid, message.RequestId);
        }
    }
}
