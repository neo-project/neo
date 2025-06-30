// Copyright (C) 2015-2025 The Neo Project.
//
// MessageRelayer.cs file belongs to the neo project and is free
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
    public class MessageRelayer : IDisposable
    {
        private volatile bool _disposed;

        private readonly Task[] _workers;
        private readonly
#if NET9_0_OR_GREATER
            Lock
#else
            object
#endif
            _lock = new();
        private readonly Queue<object> _queue = new();
        private readonly Queue<object> _queuePriority = new();
        private readonly SemaphoreSlim _semaphore = new(0);
        private delegate void OnReceiveDelegate(object message);
        private readonly Dictionary<Type, OnReceiveDelegate[]> _handlers = [];

        public MessageRelayer(int workerCount)
        {
            _workers = new Task[workerCount];
            for (var i = 0; i < workerCount; i++)
            {
                _workers[i] = Task.Run(WorkerLoop);
            }
        }

        internal void Subscribe(MessageReceiver receiver, params Type[] types)
        {
            foreach (var type in types)
            {
                if (_handlers.TryGetValue(type, out var handlers))
                {
                    Array.Resize(ref handlers, handlers.Length + 1);
                    handlers[^1] = new OnReceiveDelegate(receiver.OnReceive);

                    _handlers[type] = handlers;
                }
                else
                {
                    handlers = [receiver.OnReceive];
                    _handlers[type] = handlers;
                }
            }
        }

        private async Task WorkerLoop()
        {
            object? message;

start:
            while (!_disposed)
            {
                await _semaphore.WaitAsync();

                lock (_lock)
                {
                    if (_queuePriority.Count != 0)
                    {
                        if (!_queuePriority.TryDequeue(out message))
                        {
                            // This should happen only during Dispose
                            break;
                        }
                    }
                    else
                    {
                        if (!_queue.TryDequeue(out message))
                        {
                            // This should happen only during Dispose
                            break;
                        }
                    }
                }

                // Iterate handlers

                if (_handlers.TryGetValue(message.GetType(), out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            handler(message);
                        }
                        catch (Exception ex)
                        {
                            OnMessageError(ex);
                        }
                    }
                }
            }

            // If the receiver is disposed, exit the loop.

            if (!_disposed) goto start;
        }

        protected virtual void OnMessageError(Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
        }

        #region Single Entry

        public void Tell(object message)
        {
            lock (_lock)
            {
                _queue.Enqueue(message);
            }

            _semaphore.Release();
        }

        public void TellPriorty(object message)
        {
            lock (_lock)
            {
                _queuePriority.Enqueue(message);
            }

            _semaphore.Release();
        }

        #endregion

        #region Multiple Entries

        public void Tell(params object[] messages)
        {
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    _queue.Enqueue(message);
                }
            }

            _semaphore.Release(messages.Length);
        }

        public void TellPriority(params object[] messages)
        {
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    _queuePriority.Enqueue(message);
                }
            }

            _semaphore.Release(messages.Length);
        }

        #endregion

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
