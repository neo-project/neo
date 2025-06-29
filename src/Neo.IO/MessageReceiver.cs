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
using System.Threading.Tasks;

namespace Neo.IO
{
    public abstract class MessageReceiver(int workerCount)
        : MessageReceiver<object>(workerCount)
    { }

    public abstract class MessageReceiver<T> : IDisposable
    {
        private readonly Task[] _workers;
        private readonly BlockingCollection<T> _queue = [];

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="workerCount">Workers count</param>
        public MessageReceiver(int workerCount)
        {
            _workers = new Task[workerCount];
            for (var i = 0; i < workerCount; i++)
            {
                _workers[i] = Task.Run(WorkerLoop);
            }
        }

        /// <summary>
        /// Receive a message
        /// </summary>
        /// <param name="message">Message</param>
        public abstract void OnReceive(T message);

        private void WorkerLoop()
        {
            foreach (var message in _queue.GetConsumingEnumerable())
            {
                try
                {
                    OnReceive(message);
                }
                catch (Exception ex)
                {
                    OnMessageError(ex);
                }
            }
        }

        /// <summary>
        /// Process a message error
        /// </summary>
        /// <param name="exception">Exception</param>
        protected virtual void OnMessageError(Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
        }

        /// <summary>
        /// Dispatches a message to all registered handlers for the message's type.
        /// </summary>
        /// <param name="message">Message</param>
        public void Tell(T message) => _queue.Add(message);

        /// <summary>
        /// Dispatches a message to all registered handlers for the message's type.
        /// </summary>
        /// <param name="messages">Messages</param>
        public void Tell(params T[] messages)
        {
            foreach (var message in messages)
            {
                _queue.Add(message);
            }
        }

        /// <summary>
        /// Releases all resources used by the message dispatcher.
        /// </summary>
        public virtual void Dispose()
        {
            _queue.CompleteAdding();

            try
            {
                Task.WaitAll(_workers);
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // Expected cancellation
            }

            _queue.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
