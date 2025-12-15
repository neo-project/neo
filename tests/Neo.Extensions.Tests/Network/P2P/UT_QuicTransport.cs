// Copyright (C) 2015-2025 The Neo Project.
//
// UT_QuicTransport.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Extensions.Tests.Network.P2P
{
    [TestClass]
    public class UT_QuicTransport
    {
        private sealed class ByteCollectorActor : ReceiveActor
        {
            private readonly int _expected;
            private readonly TaskCompletionSource<IReadOnlyList<byte[]>> _tcs;
            private readonly List<byte[]> _received = new();

            public ByteCollectorActor(int expected, TaskCompletionSource<IReadOnlyList<byte[]>> tcs)
            {
                _expected = expected;
                _tcs = tcs;

                Receive<object>(msg =>
                {
                    var msgType = msg.GetType();
                    if (msgType.FullName != "Neo.Network.P2P.Transports.TransportMessages+Received")
                        return;

                    var data = msgType.GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)?.GetValue(msg);
                    if (data is not ByteString bs)
                        return;

                    _received.Add(bs.ToArray());
                    if (_received.Count == _expected)
                        _tcs.TrySetResult(_received.ToArray());
                });
            }
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        private static QuicClientConnectionOptions CreateInsecureTestClientOptions(IPEndPoint remoteEndPoint)
        {
            return new QuicClientConnectionOptions
            {
                RemoteEndPoint = remoteEndPoint,
                DefaultCloseErrorCode = 0,
                DefaultStreamErrorCode = 0,
                MaxInboundBidirectionalStreams = 32,
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { new("neo-p2p") },
                    TargetHost = "neo-p2p",
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (_, _, _, _) => true,
                }
            };
        }

        private static async Task<object> CreateQuicTransportListenerAsync(IPEndPoint listenEndPoint, CancellationToken cancellationToken)
        {
            var neoAssembly = typeof(ChannelsConfig).Assembly;
            var listenerType = neoAssembly.GetType("Neo.Network.P2P.Transports.QuicTransportListener", throwOnError: true)!;
            var listenAsync = listenerType.GetMethod("ListenAsync", BindingFlags.Public | BindingFlags.Static)!;
            var taskObj = listenAsync.Invoke(null, new object[] { listenEndPoint, cancellationToken })!;

            await (Task)taskObj;
            return taskObj.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)!.GetValue(taskObj)!;
        }

        private static async Task<object> AcceptQuicTransportConnectionAsync(object listener, CancellationToken cancellationToken)
        {
            var acceptAsync = listener.GetType().GetMethod("AcceptAsync", BindingFlags.Public | BindingFlags.Instance)!;
            var taskObj = acceptAsync.Invoke(listener, new object[] { cancellationToken })!;

            await (Task)taskObj;
            return taskObj.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)!.GetValue(taskObj)!;
        }

        private static IPEndPoint GetListenEndPoint(object listener)
        {
            return (IPEndPoint)listener.GetType().GetProperty("ListenEndPoint", BindingFlags.Public | BindingFlags.Instance)!.GetValue(listener)!;
        }

        private static void StartTransportConnection(object connection, IActorRef receiver)
        {
            connection.GetType().GetMethod("Start", BindingFlags.Public | BindingFlags.Instance)!.Invoke(connection, new object[] { receiver });
        }

        private static async Task<object> ConnectQuicTransportConnectionAsync(IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            var neoAssembly = typeof(ChannelsConfig).Assembly;
            var connectionType = neoAssembly.GetType("Neo.Network.P2P.Transports.QuicTransportConnection", throwOnError: true)!;
            var connectAsync = connectionType.GetMethod("ConnectAsync", BindingFlags.Public | BindingFlags.Static)!;
            var taskObj = connectAsync.Invoke(null, new object[] { remoteEndPoint, cancellationToken })!;

            await (Task)taskObj;
            return taskObj.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)!.GetValue(taskObj)!;
        }

        private static async Task SendQuicTransportAsync(object connection, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            var sendAsync = connection.GetType().GetMethod("SendAsync", BindingFlags.Public | BindingFlags.Instance)!;
            var taskObj = sendAsync.Invoke(connection, new object[] { data, cancellationToken })!;
            await (Task)taskObj;
        }

        [TestMethod]
        public void QuicCapability_Encoding_IsStable()
        {
            var neoAssembly = typeof(ChannelsConfig).Assembly;
            var capabilityType = neoAssembly.GetType("Neo.Network.P2P.Capabilities.NeoP2PExtensionsCapability", throwOnError: true)!;
            var dataType = neoAssembly.GetType("Neo.Network.P2P.Capabilities.NeoP2PExtensionsData", throwOnError: true)!;
            var extensionsEnum = neoAssembly.GetType("Neo.Network.P2P.Capabilities.NeoP2PExtensions", throwOnError: true)!;

            object quicFlag = Enum.Parse(extensionsEnum, "Quic");
            object data = Activator.CreateInstance(
                dataType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { quicFlag, (ushort)12345 },
                culture: null)!;
            var create = capabilityType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!;

            var unknown = (UnknownCapability)create.Invoke(null, new[] { data })!;

            Assert.AreEqual(NodeCapabilityType.Extension0, unknown.Type);
            Assert.AreEqual(8, unknown.Data.Length);

            Assert.AreEqual((byte)'N', unknown.Data.Span[0]);
            Assert.AreEqual((byte)'E', unknown.Data.Span[1]);
            Assert.AreEqual((byte)'O', unknown.Data.Span[2]);
            Assert.AreEqual((byte)'Q', unknown.Data.Span[3]);
            Assert.AreEqual(1, unknown.Data.Span[4]); // capability version
            Assert.AreEqual(1, unknown.Data.Span[5]); // quic flag
            Assert.AreEqual((ushort)12345, BinaryPrimitives.ReadUInt16LittleEndian(unknown.Data.Span[6..8]));
        }

        [TestMethod]
        public void QuicCapability_TryParse_ReturnsExpectedData()
        {
            var neoAssembly = typeof(ChannelsConfig).Assembly;
            var capabilityType = neoAssembly.GetType("Neo.Network.P2P.Capabilities.NeoP2PExtensionsCapability", throwOnError: true)!;
            var dataType = neoAssembly.GetType("Neo.Network.P2P.Capabilities.NeoP2PExtensionsData", throwOnError: true)!;
            var extensionsEnum = neoAssembly.GetType("Neo.Network.P2P.Capabilities.NeoP2PExtensions", throwOnError: true)!;

            object quicFlag = Enum.Parse(extensionsEnum, "Quic");
            object data = Activator.CreateInstance(
                dataType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { quicFlag, (ushort)23456 },
                culture: null)!;

            var create = capabilityType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!;
            var unknown = (UnknownCapability)create.Invoke(null, new[] { data })!;

            var tryParse = capabilityType.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, binder: null, types: new[] { typeof(NodeCapability[]), dataType.MakeByRefType() }, modifiers: null)!;
            object? parsed = null;
            var args = new object?[] { new NodeCapability[] { unknown }, parsed };

            var ok = (bool)tryParse.Invoke(null, args)!;
            Assert.IsTrue(ok);

            var parsedData = args[1]!;
            var parsedExtensions = parsedData.GetType().GetProperty("Extensions", BindingFlags.Public | BindingFlags.Instance)!.GetValue(parsedData)!;
            var parsedPort = (ushort)parsedData.GetType().GetProperty("QuicPort", BindingFlags.Public | BindingFlags.Instance)!.GetValue(parsedData)!;

            Assert.AreEqual(quicFlag, parsedExtensions);
            Assert.AreEqual((ushort)23456, parsedPort);
        }

        [TestMethod]
        public void QuicTransport_CreateSelfSignedCertificate_HasPrivateKeyAndEku()
        {
            var neoAssembly = typeof(ChannelsConfig).Assembly;
            var transportType = neoAssembly.GetType("Neo.Network.P2P.Transports.QuicTransport", throwOnError: true)!;
            var create = transportType.GetMethod("CreateSelfSignedCertificate", BindingFlags.Public | BindingFlags.Static)!;

            using var cert = (X509Certificate2)create.Invoke(null, Array.Empty<object>())!;
            Assert.IsTrue(cert.HasPrivateKey);
            Assert.IsTrue(cert.Subject.Contains("CN=neo-p2p", StringComparison.OrdinalIgnoreCase));

            bool hasServerAuthEku = false;
            foreach (var ext in cert.Extensions)
            {
                if (ext is not X509EnhancedKeyUsageExtension eku)
                    continue;

                foreach (var oid in eku.EnhancedKeyUsages)
                {
                    if (oid.Value == "1.3.6.1.5.5.7.3.1")
                        hasServerAuthEku = true;
                }
            }

            Assert.IsTrue(hasServerAuthEku);
        }

        [TestMethod]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        public async Task QuicTransport_ReceivesStreamsInAcceptOrder()
        {
            if (!QuicListener.IsSupported)
                return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            using var system = ActorSystem.Create("quic-order-tests");
            try
            {
                await using var listener = (IAsyncDisposable)await CreateQuicTransportListenerAsync(
                    new IPEndPoint(IPAddress.Loopback, 0),
                    cts.Token);
                var listenEndPoint = GetListenEndPoint(listener);

                var acceptTask = AcceptQuicTransportConnectionAsync(listener, cts.Token);
                await using var client = await QuicConnection.ConnectAsync(CreateInsecureTestClientOptions(listenEndPoint), cts.Token);
                await using var serverConnection = (IAsyncDisposable)await acceptTask;

                var receivedTcs = new TaskCompletionSource<IReadOnlyList<byte[]>>(TaskCreationOptions.RunContinuationsAsynchronously);
                var receiver = system.ActorOf(Props.Create(() => new ByteCollectorActor(2, receivedTcs)));
                StartTransportConnection(serverConnection, receiver);

                await using var s1 = await client.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cts.Token);
                await using var s2 = await client.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cts.Token);

                var msg1 = new byte[] { 1, 1, 1 };
                var msg2 = new byte[] { 2, 2, 2 };

                await s2.WriteAsync(msg2, cts.Token);
                s2.CompleteWrites();

                await Task.Delay(100, cts.Token);

                await s1.WriteAsync(msg1, cts.Token);
                s1.CompleteWrites();

                var received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), cts.Token);
                Assert.AreEqual(2, received.Count);
                CollectionAssert.AreEqual(msg1, received[0]);
                CollectionAssert.AreEqual(msg2, received[1]);
            }
            finally
            {
                await system.Terminate();
            }
        }

        [TestMethod]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        public async Task QuicTransportConnection_SendAsync_Roundtrip()
        {
            if (!QuicListener.IsSupported)
                return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var system = ActorSystem.Create("quic-send-tests");

            try
            {
                await using var listener = (IAsyncDisposable)await CreateQuicTransportListenerAsync(
                    new IPEndPoint(IPAddress.Loopback, 0),
                    cts.Token);
                var listenEndPoint = GetListenEndPoint(listener);

                var acceptTask = AcceptQuicTransportConnectionAsync(listener, cts.Token);
                await using var clientConnection = (IAsyncDisposable)await ConnectQuicTransportConnectionAsync(listenEndPoint, cts.Token);
                await using var serverConnection = (IAsyncDisposable)await acceptTask;

                var receivedTcs = new TaskCompletionSource<IReadOnlyList<byte[]>>(TaskCreationOptions.RunContinuationsAsynchronously);
                var receiver = system.ActorOf(Props.Create(() => new ByteCollectorActor(3, receivedTcs)));
                StartTransportConnection(serverConnection, receiver);

                var msg1 = new byte[] { 10, 11, 12 };
                var msg2 = new byte[] { 20, 21, 22 };
                var msg3 = new byte[] { 30, 31, 32 };

                await SendQuicTransportAsync(clientConnection, msg1, cts.Token);
                await SendQuicTransportAsync(clientConnection, msg2, cts.Token);
                await SendQuicTransportAsync(clientConnection, msg3, cts.Token);

                var received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), cts.Token);
                Assert.AreEqual(3, received.Count);
                CollectionAssert.AreEqual(msg1, received[0]);
                CollectionAssert.AreEqual(msg2, received[1]);
                CollectionAssert.AreEqual(msg3, received[2]);
            }
            finally
            {
                await system.Terminate();
            }
        }

        [TestMethod]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        public async Task QuicTransport_MultiplexesMultipleMessages()
        {
            if (!QuicListener.IsSupported)
                return;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            using var system = ActorSystem.Create("quic-mux-tests");
            try
            {
                await using var listener = (IAsyncDisposable)await CreateQuicTransportListenerAsync(
                    new IPEndPoint(IPAddress.Loopback, 0),
                    cts.Token);
                var listenEndPoint = GetListenEndPoint(listener);

                var acceptTask = AcceptQuicTransportConnectionAsync(listener, cts.Token);
                await using var client = await QuicConnection.ConnectAsync(CreateInsecureTestClientOptions(listenEndPoint), cts.Token);
                await using var serverConnection = (IAsyncDisposable)await acceptTask;

                var receivedTcs = new TaskCompletionSource<IReadOnlyList<byte[]>>(TaskCreationOptions.RunContinuationsAsynchronously);
                var receiver = system.ActorOf(Props.Create(() => new ByteCollectorActor(5, receivedTcs)));
                StartTransportConnection(serverConnection, receiver);

                var expected = new List<byte[]>();
                for (int i = 0; i < 5; i++)
                {
                    var payload = new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) };
                    expected.Add(payload);
                    await using var stream = await client.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cts.Token);
                    await stream.WriteAsync(payload, cts.Token);
                    stream.CompleteWrites();
                }

                var received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), cts.Token);
                Assert.AreEqual(5, received.Count);
                for (int i = 0; i < 5; i++)
                    CollectionAssert.AreEqual(expected[i], received[i]);
            }
            finally
            {
                await system.Terminate();
            }
        }
    }
}
