// Copyright (C) 2015-2025 The Neo Project.
//
// NeoNetConnection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using Neo.Network.P2P;
using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Neo.IO.Network
{
    internal class NeoNetConnection : IAsyncDisposable, IDisposable
    {
        internal const int MinAllocBufferSize = 4096;
        internal const int MaxMessageCapacity = 4096;

        private readonly Channel<Message> _messageQueue; // queue for messages

        private readonly Socket _socket;
        private readonly IPEndPoint _endPoint;

        private readonly CancellationTokenSource _connectionClosedTokenSource = new();
        private readonly CancellationToken _connectionClosedToken = default;


        private readonly IDuplexPipe _originalTransport;
        private readonly object _shutdownLock = new();

        private Task _receivingTask = Task.CompletedTask;
        private Task _sendingTask = Task.CompletedTask;
        private Task _processMessageTask = Task.CompletedTask;

        private Exception _shutdownReason;

        private bool _connectionClosed;
        private bool _connectionShutdown;
        private bool _streamDisconnected;

        internal PipeWriter Input => Application.Output;

        internal PipeReader Output => Application.Input;

        internal PipeWriter Writer => Transport.Output;

        internal PipeReader Reader => Transport.Input;

        internal IDuplexPipe Application { get; private set; }

        public IDuplexPipe Transport { get; private set; }

        public NeoNetConnection(
            IPEndPoint endPoint,
            Socket socket,
            PipeOptions inputOptions,
            PipeOptions outputOptions)
        {
            _endPoint = endPoint;
            _socket = socket;

            _connectionClosedToken = _connectionClosedTokenSource.Token;
            _messageQueue = Channel.CreateUnbounded<Message>(
                new UnboundedChannelOptions()
                {
                    SingleReader = true,
                });

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            Transport = _originalTransport = pair.Transport;
            Application = pair.Application;
        }

        /// <summary>
        /// Releases the resources used by the current connection.
        /// </summary>
        /// <remarks>This method synchronously disposes of resources by invoking the asynchronous disposal
        /// logic.  It should be called when the instance is no longer needed to ensure proper cleanup of
        /// resources.</remarks>
        public void Dispose()
        {
            DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously releases the resources used by the current connection of <see cref="NeoNetConnection"/>.
        /// </summary>
        /// <remarks>This method completes the input and output streams associated with the connection and
        /// ensures that  all background tasks related to message processing are awaited. It also disposes of the
        /// underlying  server stream or stops the listener, depending on the connection state.</remarks>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _originalTransport.Input.Complete();
            _originalTransport.Output.Complete();

            try
            {
                await _receivingTask;
                await _sendingTask;
                await _processMessageTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("-----------------------------------------------------");
                Debug.WriteLine($"Unexpected exception in {nameof(NeoNetConnection)}.{nameof(DisposeAsync)}.");
                Debug.WriteLine(ex);
                Debug.WriteLine("-----------------------------------------------------");
                _socket.Dispose();
            }

            if (_streamDisconnected == false)
                _socket.Dispose();
        }

        /// <summary>
        /// Aborts the current connection and cancels any pending reads.
        /// </summary>
        /// <remarks>This method shuts down the operation using the specified abort reason and cancels any
        /// pending reads  on both the output and reader streams. Ensure that <paramref name="abortReason"/> provides
        /// meaningful  context for the abort to aid in debugging or error handling.</remarks>
        /// <param name="abortReason">The exception that indicates the reason for the abort. Cannot be <see langword="null"/>.</param>
        public void Abort(Exception abortReason)
        {
            Shutdown(abortReason);

            Output.CancelPendingRead();
            Reader.CancelPendingRead();
        }

        /// <summary>
        /// Asynchronously reads a message from the queue.
        /// </summary>
        /// <remarks>This method waits until a message is available in the queue and then retrieves it. If
        /// the queue is closed and no new messages are added, the method will return <see langword="null"/>.</remarks>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will be canceled if the token is triggered.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation. The result is the next <see
        /// cref="Message"/> in the queue, or <see langword="null"/> if no messages are available.</returns>
        public async ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            while (await _messageQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_messageQueue.Reader.TryRead(out var message))
                    return message;
            }

            return null;
        }

        public async Task TellAsync(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                if (buffer.IsEmpty)
                    return;

                if (Message.TryDeserialize(ByteString.FromBytes(buffer.ToArray()), out var message) <= 0)
                    return;

                if (_messageQueue.Writer.TryWrite(message) == false)
                {
                    if (await _messageQueue.Writer.WaitToWriteAsync(_connectionClosedToken) == false)
                        throw new InvalidOperationException("Message queue writer was unexpectedly closed.");
                }
            }
            catch (IndexOutOfRangeException) // NULL message or Empty message
            {
                Debug.WriteLine("-----------------------------------------------------");
                Debug.WriteLine($"Received a corrupted message in {nameof(NeoNetConnection)}.{nameof(TellAsync)}.");
                Debug.WriteLine("-----------------------------------------------------");
            }
            catch (FormatException ex) // Normally invalid or corrupt message
            {
                Debug.WriteLine("-----------------------------------------------------");
                Debug.WriteLine($"Format exception in {nameof(NeoNetConnection)}.{nameof(TellAsync)}.");
                Debug.WriteLine(ex);
                Debug.WriteLine("-----------------------------------------------------");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("-----------------------------------------------------");
                Debug.WriteLine($"Unexpected exception in {nameof(NeoNetConnection)}.{nameof(TellAsync)}.");
                Debug.WriteLine(ex);
                Debug.WriteLine("-----------------------------------------------------");
            }
        }

        internal void Start()
        {
            try
            {
                _receivingTask = DoReceiveAsync();
                _sendingTask = DoSendAsync();
                _processMessageTask = ProcessMessagesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("-----------------------------------------------------");
                Debug.WriteLine($"Unexpected exception in {nameof(NeoNetConnection)}.{nameof(Start)}.");
                Debug.WriteLine(ex);
                Debug.WriteLine("-----------------------------------------------------");
            }
        }

        private void Shutdown(Exception shutdownReason)
        {
            lock (_shutdownLock)
            {
                if (_connectionShutdown)
                    return;

                _connectionShutdown = true;

                _shutdownReason = shutdownReason;

                try
                {
                    _socket.Close();
                    _streamDisconnected = true;
                }
                catch
                {
                }
            }
        }

        private async Task DoReceiveAsync()
        {
            Exception error = null;

            try
            {
                var input = Input;

                while (true)
                {
                    var buffer = input.GetMemory(MinAllocBufferSize);
                    var bytesReceived = await _socket.ReceiveAsync(buffer, SocketFlags.None);

                    if (bytesReceived == 0)
                        break;

                    input.Advance(bytesReceived);

                    var result = await input.FlushAsync();

                    if (result.IsCompleted || result.IsCanceled)
                        break;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                Input.Complete(_shutdownReason ?? error);
                FireConnectionClosed();
            }
        }

        private async Task DoSendAsync()
        {
            Exception shutdownReason = null;
            Exception unexpectedError = null;

            try
            {
                while (true)
                {
                    var result = await Output.ReadAsync();

                    if (result.IsCanceled)
                        break;

                    var buffer = result.Buffer;
                    if (buffer.IsSingleSegment)
                        await _socket.SendAsync(buffer.First, SocketFlags.None);
                    else
                    {
                        foreach (var segment in buffer)
                            await _socket.SendAsync(segment, SocketFlags.None);
                    }

                    Output.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                        break;
                }
            }
            catch (ObjectDisposedException ex)
            {
                shutdownReason = ex;
            }
            catch (Exception ex)
            {
                shutdownReason = ex;
                unexpectedError = ex;
            }
            finally
            {
                Shutdown(shutdownReason);

                Output.Complete(unexpectedError);
                Input.CancelPendingFlush();
            }
        }

        private void FireConnectionClosed()
        {
            lock (_shutdownLock)
            {
                if (_connectionClosed)
                    return;

                _connectionClosed = true;
            }

            CancelConnectionClosedToken();
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("-----------------------------------------------------");
                Debug.WriteLine($"Unexpected exception in {nameof(NeoNetConnection)}.{nameof(CancelConnectionClosedToken)}.");
                Debug.WriteLine(ex);
                Debug.WriteLine("-----------------------------------------------------");
            }
        }

        private async Task ProcessMessagesAsync()
        {
            Exception unexpectedError = null;

            try
            {
                while (true)
                {
                    var result = await Reader.ReadAsync();

                    if (result.IsCanceled)
                        break;

                    var buffer = result.Buffer;
                    if (buffer.IsSingleSegment)
                        await TellAsync(buffer.First);
                    else
                    {
                        foreach (var segment in buffer)
                            await TellAsync(segment);
                    }

                    Reader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                        break;
                }
            }
            catch (Exception ex)
            {
                unexpectedError = ex;

                Debug.WriteLine("-----------------------------------------------------");
                Debug.WriteLine($"Unexpected exception in {nameof(NeoNetConnection)}.{nameof(ProcessMessagesAsync)}.");
                Debug.WriteLine(ex);
                Debug.WriteLine("-----------------------------------------------------");
            }
            finally
            {
                Shutdown(unexpectedError);

                Reader.Complete(unexpectedError);
                Output.CancelPendingRead();

                _messageQueue.Writer.Complete(unexpectedError);
            }
        }
    }
}
