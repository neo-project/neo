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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.IO
{
    internal abstract class MessageReceiver(int workerCount)
        : MessageReceiver<object>(workerCount)
    { }

    internal abstract class MessageReceiver<T> : IDisposable
    {
        private readonly Task[] _workers;
        private readonly Queue<T> _queue = new();
        private readonly SemaphoreSlim _semaphore = new(0);
        private volatile bool _disposed;

        public MessageReceiver(int workerCount)
        {
            _workers = new Task[workerCount];
            for (var i = 0; i < workerCount; i++)
            {
                _workers[i] = Task.Run(WorkerLoop);
            }
        }

        public abstract void OnReceive(T message);

        private async Task WorkerLoop()
        {
            T? message;

start:
            try
            {
                while (!_disposed)
                {
                    await _semaphore.WaitAsync();

                    lock (_queue)
                    {
                        if (!_queue.TryDequeue(out message))
                        {
                            // This should happen only during Dispose
                            break;
                        }
                    }

                    OnReceive(message);
                }
            }
            catch (Exception ex)
            {
                OnMessageError(ex);
            }

            // If the receiver is disposed, exit the loop.

            if (!_disposed) goto start;
        }

        protected virtual void OnMessageError(Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
        }

        public void Tell(T message)
        {
            lock (_queue)
            {
                _queue.Enqueue(message);
            }

            _semaphore.Release();
        }

        public void Tell(params T[] messages)
        {
            lock (_queue)
            {
                foreach (var message in messages)
                {
                    _queue.Enqueue(message);
                }
            }

            _semaphore.Release(messages.Length);
        }

        public virtual void Dispose()
        {
            _disposed = true;

            for (var i = 0; i < _workers.Length; i++)
            {
                _semaphore.Release();
            }

            try
            {
                Task.WaitAll(_workers);
            }
            catch (OperationCanceledException) { }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException)) { }

            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
