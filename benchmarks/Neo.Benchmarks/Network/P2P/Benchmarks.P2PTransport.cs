// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.P2PTransport.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Benchmark.Network.P2P
{
    [MemoryDiagnoser]
    public class Benchmarks_P2PTransport
    {
        [SupportedOSPlatformGuard("linux")]
        [SupportedOSPlatformGuard("macos")]
        [SupportedOSPlatformGuard("windows")]
        private static bool IsQuicPlatformSupported =>
            OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

        [Params(256, 1024, 8192, 65536)]
        public int MessageSize;

        [Params(256)]
        public int MessagesPerIteration;

        private byte[] _payload = Array.Empty<byte>();

        private CancellationTokenSource _cts;

        private TcpListener _tcpListener;
        private TcpClient _tcpClient;
        private TcpClient _tcpServerClient;
        private NetworkStream _tcpClientStream;
        private NetworkStream _tcpServerStream;
        private Task _tcpReadLoop;
        private long _tcpReadTargetBytes;
        private long _tcpReadSoFar;
        private readonly ManualResetEventSlim _tcpReadDone = new(false);

        private QuicListener _quicListener;
        private QuicConnection _quicClient;
        private QuicConnection _quicServer;
        private X509Certificate2 _quicCert;
        private Task _quicAcceptLoop;
        private int _quicExpectedMessages;
        private int _quicMessagesReceived;
        private readonly ManualResetEventSlim _quicReadDone = new(false);

        [GlobalSetup]
        public async Task SetupAsync()
        {
            _payload = new byte[MessageSize];
            RandomNumberGenerator.Fill(_payload);

            _cts = new CancellationTokenSource();

            SetupTcp();
            if (IsQuicPlatformSupported)
                await SetupQuicAsync().ConfigureAwait(false);
        }

        [GlobalCleanup]
        public async Task CleanupAsync()
        {
            _cts.Cancel();

            _tcpReadDone.Set();
            _quicReadDone.Set();

            try { _tcpClientStream.Dispose(); } catch { }
            try { _tcpServerStream.Dispose(); } catch { }
            try { _tcpClient.Dispose(); } catch { }
            try { _tcpServerClient.Dispose(); } catch { }
            try { _tcpListener.Stop(); } catch { }

            if (IsQuicPlatformSupported && QuicListener.IsSupported)
            {
                try { await _quicClient.DisposeAsync().ConfigureAwait(false); } catch { }
                try { await _quicServer.DisposeAsync().ConfigureAwait(false); } catch { }
                try { await _quicListener.DisposeAsync().ConfigureAwait(false); } catch { }
                try { _quicCert.Dispose(); } catch { }
            }

            _cts.Dispose();
        }

        private void SetupTcp()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 0);
            _tcpListener.Start();

            _tcpClient = new TcpClient { NoDelay = true };
            _tcpClient.Connect((IPEndPoint)_tcpListener.LocalEndpoint);
            _tcpClientStream = _tcpClient.GetStream();

            _tcpServerClient = _tcpListener.AcceptTcpClient();
            _tcpServerClient.NoDelay = true;
            _tcpServerStream = _tcpServerClient.GetStream();

            _tcpReadLoop = Task.Run(() => TcpReadLoopAsync(_tcpServerStream, _cts!.Token));
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        private async Task SetupQuicAsync()
        {
            if (!IsQuicPlatformSupported || !QuicListener.IsSupported)
                return;

            _quicCert = CreateSelfSignedCertificate("neo-p2p");

            var listenerOptions = new QuicListenerOptions
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ApplicationProtocols = new List<SslApplicationProtocol> { new("neo-p2p") },
                ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(new QuicServerConnectionOptions
                {
                    DefaultCloseErrorCode = 0,
                    DefaultStreamErrorCode = 0,
                    MaxInboundBidirectionalStreams = 2048,
                    ServerAuthenticationOptions = new SslServerAuthenticationOptions
                    {
                        ApplicationProtocols = new List<SslApplicationProtocol> { new("neo-p2p") },
                        ServerCertificate = _quicCert,
                    },
                })
            };

            _quicListener = await QuicListener.ListenAsync(listenerOptions, _cts!.Token).ConfigureAwait(false);

            var serverAcceptTask = _quicListener.AcceptConnectionAsync(_cts.Token).AsTask();

            var clientOptions = new QuicClientConnectionOptions
            {
                RemoteEndPoint = (IPEndPoint)_quicListener.LocalEndPoint,
                DefaultCloseErrorCode = 0,
                DefaultStreamErrorCode = 0,
                MaxInboundBidirectionalStreams = 2048,
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = new List<SslApplicationProtocol> { new("neo-p2p") },
                    TargetHost = "neo-p2p",
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (_, _, _, _) => true,
                }
            };

            _quicClient = await QuicConnection.ConnectAsync(clientOptions, _cts.Token).ConfigureAwait(false);
            _quicServer = await serverAcceptTask.ConfigureAwait(false);

            _quicAcceptLoop = Task.Run(() => QuicAcceptLoopAsync(_quicServer, _cts.Token), _cts.Token);
        }

        private async Task TcpReadLoopAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[64 * 1024];
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;

                    var total = Interlocked.Add(ref _tcpReadSoFar, read);
                    if (total >= Volatile.Read(ref _tcpReadTargetBytes))
                        _tcpReadDone.Set();
                }
            }
            catch
            {
            }
        }

        [Benchmark(Description = "TCP: single stream, N fixed-size messages")]
        public void Tcp_SendFixedSizeMessages()
        {
            var stream = _tcpClientStream;
            _tcpReadDone.Reset();
            Interlocked.Exchange(ref _tcpReadSoFar, 0);
            Volatile.Write(ref _tcpReadTargetBytes, (long)_payload.Length * MessagesPerIteration);

            for (int i = 0; i < MessagesPerIteration; i++)
                stream.Write(_payload);

            stream.Flush();
            _tcpReadDone.Wait();
        }

        [Benchmark(Description = "QUIC: one stream per message, N fixed-size messages")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        public async Task Quic_SendFixedSizeMessages_StreamPerMessage()
        {
            if (!IsQuicPlatformSupported || !QuicListener.IsSupported)
                return;

            _quicReadDone.Reset();
            Interlocked.Exchange(ref _quicMessagesReceived, 0);
            Volatile.Write(ref _quicExpectedMessages, MessagesPerIteration);

            for (int i = 0; i < MessagesPerIteration; i++)
            {
                await using var stream = await _quicClient.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, _cts.Token).ConfigureAwait(false);
                await stream.WriteAsync(_payload, _cts.Token).ConfigureAwait(false);
                stream.CompleteWrites();
            }

            _quicReadDone.Wait();
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        private async Task QuicAcceptLoopAsync(QuicConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var stream = await connection.AcceptInboundStreamAsync(cancellationToken).ConfigureAwait(false);
                    _ = Task.Run(() => QuicDrainStreamAsync(stream, cancellationToken), cancellationToken);
                }
            }
            catch
            {
            }
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        private async Task QuicDrainStreamAsync(QuicStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[64 * 1024];
            try
            {
                while (true)
                {
                    var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                }

                var received = Interlocked.Increment(ref _quicMessagesReceived);
                if (received >= Volatile.Read(ref _quicExpectedMessages))
                    _quicReadDone.Set();
            }
            catch
            {
            }
            finally
            {
                try { await stream.DisposeAsync().ConfigureAwait(false); } catch { }
            }
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string host)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={host}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(host);
            request.CertificateExtensions.Add(sanBuilder.Build());

            using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            var pfx = cert.Export(X509ContentType.Pfx);
            var keyStorageFlags =
                OperatingSystem.IsWindows()
                    ? X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
                    : X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;

            try
            {
                return X509CertificateLoader.LoadPkcs12(pfx, password: null, keyStorageFlags: keyStorageFlags);
            }
            catch (CryptographicException) when (OperatingSystem.IsWindows())
            {
                return X509CertificateLoader.LoadPkcs12(
                    pfx,
                    password: null,
                    keyStorageFlags: X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);
            }
        }
    }
}
