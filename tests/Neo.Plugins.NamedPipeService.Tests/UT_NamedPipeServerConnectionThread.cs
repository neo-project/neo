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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Plugins.Models;
using Neo.Plugins.Models.Payloads;
using Neo.SmartContract.Manifest;
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
        public async Task Send_Random_Data()
        {
            var damagedMessage = new byte[PipeMessage.HeaderSize];
            Random.Shared.NextBytes(damagedMessage);

            var writeTask = _clientConnection.WriteAsync(damagedMessage);

            var buffer = new byte[1024];
            // No data should be returned
            Assert.ThrowsExceptionAsync<TimeoutException>(async () =>
                    await _clientConnection.ReadAsync(buffer).TimeoutAfter(TimeSpan.FromSeconds(1)));
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
            var payload = message.Payload as PipeUnmanagedPayload<uint>;

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.BlockHeight, message.Command);
            Assert.AreEqual(rid, message.RequestId);
            Assert.IsInstanceOfType<PipeUnmanagedPayload<uint>>(message.Payload);
            Assert.AreEqual(0u, payload.Value);

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
            var payload = message.Payload as PipeSerializablePayload<Block>;

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.Block, message.Command);
            Assert.AreEqual(rid, message.RequestId);
            Assert.IsInstanceOfType<PipeSerializablePayload<Block>>(message.Payload);
            Assert.AreEqual(0u, payload.Value.Index);
        }

        [TestMethod]
        public async Task SendAndReceive_Messages_GetTransaction()
        {
            var rid = Random.Shared.Next();

            var getBlockPayload = PipeMessage.Create(rid, PipeCommand.GetTransaction, new PipeSerializablePayload<UInt256>()
            {
                Value = UInt256.Zero,
            });

            var writeTask = _clientConnection.WriteAsync(getBlockPayload.ToArray());

            var buffer = new byte[1024];
            var count = await _clientConnection.ReadAsync(buffer).DefaultTimeout();

            var message = PipeMessage.Create(buffer);
            var payload = message.Payload as PipeSerializablePayload<Transaction>;

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.Transaction, message.Command);
            Assert.AreEqual(rid, message.RequestId);
            Assert.IsInstanceOfType<PipeSerializablePayload<Transaction>>(message.Payload);
            Assert.IsNull(payload.Value);
        }

        [TestMethod]
        public async Task SendAndReceive_Messages_GetMemoryPoolUnVerified()
        {
            var rid = Random.Shared.Next();

            var getBlockPayload = PipeMessage.Create(rid, PipeCommand.GetMemoryPoolUnVerified, PipeMessage.Null);

            var writeTask = _clientConnection.WriteAsync(getBlockPayload.ToArray());

            var buffer = new byte[1024];
            var count = await _clientConnection.ReadAsync(buffer).DefaultTimeout();

            var message = PipeMessage.Create(buffer);
            var memPool = message.Payload as PipeArrayPayload<PipeSerializablePayload<Transaction>>;

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.MemoryPoolUnVerified, message.Command);
            Assert.AreEqual(rid, message.RequestId);
            Assert.IsInstanceOfType<PipeArrayPayload<PipeSerializablePayload<Transaction>>>(message.Payload);
            Assert.AreEqual(0, memPool.Value.Length);
        }

        [TestMethod]
        public async Task SendAndReceive_Messages_GetMemoryPoolVerified()
        {
            var rid = Random.Shared.Next();

            var getBlockPayload = PipeMessage.Create(rid, PipeCommand.GetMemoryPoolVerified, PipeMessage.Null);

            var writeTask = _clientConnection.WriteAsync(getBlockPayload.ToArray());

            var buffer = new byte[1024];
            var count = await _clientConnection.ReadAsync(buffer).DefaultTimeout();

            var message = PipeMessage.Create(buffer);
            var memPool = message.Payload as PipeArrayPayload<PipeSerializablePayload<Transaction>>;

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.MemoryPoolVerified, message.Command);
            Assert.AreEqual(rid, message.RequestId);
            Assert.IsInstanceOfType<PipeArrayPayload<PipeSerializablePayload<Transaction>>>(message.Payload);
            Assert.AreEqual(0, memPool.Value.Length);
        }

        [TestMethod]
        public async Task SendAndReceive_Messages_GetState()
        {
            var rid = Random.Shared.Next();

            var getBlockPayload = PipeMessage.Create(rid, PipeCommand.GetState, PipeMessage.Null);

            var writeTask = _clientConnection.WriteAsync(getBlockPayload.ToArray());

            var buffer = new byte[1024];
            var count = await _clientConnection.ReadAsync(buffer).DefaultTimeout();

            var message = PipeMessage.Create(buffer);
            var remoteNodes = message.Payload as PipeArrayPayload<PipeShowStatePayload>;

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.State, message.Command);
            Assert.AreEqual(rid, message.RequestId);
            Assert.IsInstanceOfType<PipeArrayPayload<PipeShowStatePayload>>(message.Payload);
            Assert.AreEqual(0, remoteNodes.Value.Length);
        }

        [TestMethod]
        public async Task SendAndReceive_Messages_GetContractState()
        {
            var rid = Random.Shared.Next();

            var getBlockPayload = PipeMessage.Create(rid, PipeCommand.GetContractState, new PipeSerializablePayload<UInt160>() { Value = UInt160.Parse("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5") });

            var writeTask = _clientConnection.WriteAsync(getBlockPayload.ToArray());

            var buffer = new byte[4096];
            var count = await _clientConnection.ReadAsync(buffer).DefaultTimeout();

            var message = PipeMessage.Create(buffer);
            var contractState = message.Payload as PipeContractState;

            Assert.AreNotEqual(0, message.Size);
            Assert.AreEqual(PipeCommand.ContractState, message.Command);
            Assert.AreEqual(rid, message.RequestId);
            Assert.IsInstanceOfType<PipeContractState>(message.Payload);
            Assert.AreEqual(UInt160.Parse("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5"), contractState.Hash);
            Assert.AreEqual(-5, contractState.Id);
            Assert.AreEqual(0, contractState.UpdateCounter);
            Assert.AreNotEqual(0, contractState.Nef.Size);
            Assert.AreEqual("neo-core-v3.0", contractState.Nef.Compiler);
            Assert.AreEqual(1325686241u, contractState.Nef.CheckSum);
            Assert.IsInstanceOfType<ContractManifest>(contractState.Manifest);
        }
    }
}
