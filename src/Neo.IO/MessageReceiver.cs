// Copyright (C) 2015-2025 The Neo Project.
//
// MessageReceiver.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.IO
{
    public abstract class MessageReceiver<T> : IDisposable
    {
        private readonly Task[] _workers;
        private readonly SemaphoreSlim _semaphore;
        private readonly BlockingCollection<T> _queue = [];
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="workerCount">Workers count</param>
        /// <param name="maxConcurrentMessages">Max Concurrent Messages</param>
        public MessageReceiver(int workerCount, int maxConcurrentMessages = 1)
        {
            _semaphore = new SemaphoreSlim(maxConcurrentMessages);

            _workers = new Task[workerCount];
            for (var i = 0; i < workerCount; i++)
            {
                _workers[i] = Task.Run(() => WorkerLoopAsync(_cts.Token));
            }
        }

        /// <summary>
        /// Receive a message
        /// </summary>
        /// <param name="message">Message</param>
        public abstract Task OnMessageAsync(T message);

        private async Task WorkerLoopAsync(CancellationToken token)
        {
            try
            {
                foreach (var message in _queue.GetConsumingEnumerable(token))
                {
                    await DispatchInternalAsync(message).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation
            }
        }

        /// <summary>
        /// Dispatches a message to all registered handlers for the message's type.
        /// </summary>
        /// <param name="message">Message</param>
        public void Tell(T message)
        {
            _queue.Add(message);
        }

        /// <summary>
        /// Dispatches a message to all registered handlers for the message's type.
        /// </summary>
        /// <param name="messages">Messages</param>
        public void TellAll(params T[] messages)
        {
            foreach (var message in messages)
            {
                Tell(message);
            }
        }

        private async Task DispatchInternalAsync(T message)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await OnMessageAsync(message).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Releases all resources used by the message dispatcher.
        /// </summary>
        public virtual void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();

            try
            {
                Task.WaitAll(_workers);
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // Expected cancellation
            }

            _semaphore.Dispose();
            _queue.Dispose();
            _cts.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
