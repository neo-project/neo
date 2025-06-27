// Copyright (C) 2015-2025 The Neo Project.
//
// MessageDispatcher.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Neo.IO
{
    public abstract class MessageReceiver<T> : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly List<Thread> _workers = [];
        private readonly BlockingCollection<T> _queue = [];

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="workerCount">Workers count</param>
        /// <param name="maxConcurrentMessages">Max Concurrent Messages</param>
        public MessageReceiver(int workerCount, int maxConcurrentMessages = 1)
        {
            _semaphore = new SemaphoreSlim(maxConcurrentMessages);

            for (var i = 0; i < workerCount; i++)
            {
                var thread = new Thread(WorkerLoop)
                {
                    IsBackground = true
                };
                thread.Start();
                _workers.Add(thread);
            }
        }

        /// <summary>
        /// Receive a message
        /// </summary>
        /// <param name="message">Message</param>
        public abstract void OnMessage(T message);

        private void WorkerLoop()
        {
            foreach (var message in _queue.GetConsumingEnumerable())
            {
                DispatchInternal(message);
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

        private void DispatchInternal(T message)
        {
            _semaphore.Wait();

            try
            {
                OnMessage(message);
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
            _semaphore.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
