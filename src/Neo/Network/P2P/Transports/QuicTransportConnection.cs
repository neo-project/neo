// Copyright (C) 2015-2025 The Neo Project.
//
// QuicTransportConnection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Quic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network.P2P.Transports
{
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    internal sealed class QuicTransportConnection : ITransportConnection
    {
        private const int ReceiveBufferSize = 64 * 1024;
        private const int MaxMessageSize = Neo.Network.P2P.Message.PayloadMaxSize + 64;

        private readonly QuicConnection _connection;

        private CancellationTokenSource? _cts;
        private Task? _acceptLoop;
        private int _closed = 0;

        private long _nextInboundSeq = 0;
        private long _nextDeliverSeq = 0;
        private readonly object _deliverLock = new();
        private readonly Dictionary<long, byte[]> _pendingBySeq = new();

        private QuicTransportConnection(QuicConnection connection)
        {
            _connection = connection;

            RemoteEndPoint = (IPEndPoint)_connection.RemoteEndPoint;
            LocalEndPoint = (IPEndPoint)_connection.LocalEndPoint;
        }

        public IPEndPoint RemoteEndPoint { get; }

        public IPEndPoint LocalEndPoint { get; }

        public static async Task<QuicTransportConnection> ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            var options = QuicTransport.CreateClientOptions(remoteEndPoint);
            var connection = await QuicConnection.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
            return new QuicTransportConnection(connection);
        }

        public static QuicTransportConnection FromAcceptedConnection(QuicConnection connection) => new(connection);

        public void Start(IActorRef receiver)
        {
            if (_acceptLoop != null) return;

            _cts = new CancellationTokenSource();
            _acceptLoop = Task.Run(() => AcceptLoopAsync(receiver, _cts.Token));
        }

        public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            QuicStream? stream = null;
            try
            {
                stream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                stream.CompleteWrites();
            }
            finally
            {
                if (stream != null)
                    await stream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public async Task CloseAsync(bool abort, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                abort = true;

            _cts?.Cancel();

            try
            {
                await _connection.CloseAsync(abort ? 1 : 0, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await DisposeAsync().ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try { await _connection.DisposeAsync().ConfigureAwait(false); } catch { }
            _cts?.Dispose();
        }

        private async Task AcceptLoopAsync(IActorRef receiver, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    QuicStream stream = await _connection.AcceptInboundStreamAsync(cancellationToken).ConfigureAwait(false);
                    var seq = Interlocked.Increment(ref _nextInboundSeq) - 1;
                    _ = Task.Run(() => ReadStreamToEndAsync(receiver, stream, seq, cancellationToken), cancellationToken);
                }
                NotifyClosed(receiver, abort: false);
            }
            catch (OperationCanceledException)
            {
                NotifyClosed(receiver, abort: false);
            }
            catch
            {
                NotifyClosed(receiver, abort: true);
            }
        }

        private async Task ReadStreamToEndAsync(IActorRef receiver, QuicStream stream, long seq, CancellationToken cancellationToken)
        {
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                using var ms = new MemoryStream();
                while (true)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;

                    if (ms.Length + read > MaxMessageSize)
                    {
                        NotifyClosed(receiver, abort: true);
                        return;
                    }

                    ms.Write(buffer, 0, read);
                }

                var data = ms.ToArray();
                if (data.Length > 0)
                    EnqueueOrdered(receiver, seq, data);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                NotifyClosed(receiver, abort: true);
            }
            finally
            {
                try { await stream.DisposeAsync().ConfigureAwait(false); } catch { }
            }
        }

        private void EnqueueOrdered(IActorRef receiver, long seq, byte[] data)
        {
            lock (_deliverLock)
            {
                _pendingBySeq[seq] = data;

                while (_pendingBySeq.TryGetValue(_nextDeliverSeq, out var next))
                {
                    _pendingBySeq.Remove(_nextDeliverSeq);
                    _nextDeliverSeq++;
                    receiver.Tell(new TransportMessages.Received(ByteString.FromBytes(next)));
                }
            }
        }

        private void NotifyClosed(IActorRef receiver, bool abort)
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0) return;
            receiver.Tell(new TransportMessages.ConnectionClosed(Abort: abort));
        }
    }
}
