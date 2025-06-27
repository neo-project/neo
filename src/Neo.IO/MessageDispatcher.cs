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
using System.Reflection;
using System.Threading;

namespace Neo.IO
{
    public class MessageDispatcher : IDisposable
    {
        private class MessageHandler(object instance, MethodInfo method)
        {
            public object Instance { get; } = instance;
            public MethodInfo Method { get; } = method;
        }

        private readonly Dictionary<Type, List<MessageHandler>> _handlers = [];
        private readonly BlockingCollection<object> _queue = [];
        private readonly List<Thread> _workers = [];

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="workerCount">Workers count</param>
        public MessageDispatcher(int workerCount)
        {
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

        private void WorkerLoop()
        {
            foreach (var message in _queue.GetConsumingEnumerable())
            {
                DispatchInternal(message);
            }
        }

        /// <summary>
        /// Registers a message handler for a specific message type.
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="handler">Handler</param>
        public void RegisterHandler<T>(IMessageHandler<T> handler)
        {
            var methodInfo = handler
                .GetType()
                .GetMethod(
                    nameof(handler.OnMessage),
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    [typeof(T)],
                    null)!;

            RegisterHandler(new MessageHandler(handler, methodInfo), typeof(T));

            if (handler is IMessageHandler objectHandler)
            {
                RegisterHandler(objectHandler);
            }
        }

        /// <summary>
        /// Registers a message handler for the object type.
        /// </summary>
        /// <param name="handler">Handler</param>
        public void RegisterHandler(IMessageHandler handler)
        {
            var methodInfo = handler
                .GetType()
                .GetMethod(
                    nameof(handler.OnMessage),
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    [typeof(object)],
                    null)!;

            RegisterHandler(new MessageHandler(handler, methodInfo), typeof(object));
        }

        private void RegisterHandler(MessageHandler handler, Type type)
        {
            if (!_handlers.TryGetValue(type, out var list))
            {
                _handlers[type] = list = [];
            }

            list.Add(handler);
        }

        /// <summary>
        /// Dispatches a message to all registered handlers for the message's type.
        /// </summary>
        /// <param name="message">Message</param>
        public void Dispatch(object message)
        {
            _queue.Add(message);
        }

        /// <summary>
        /// Dispatches a message to all registered handlers for the message's type.
        /// </summary>
        /// <param name="messages">Messages</param>
        public void DispatchAll(params object[] messages)
        {
            foreach (var message in messages)
            {
                _queue.Add(messages);
            }
        }

        private void DispatchInternal(object message)
        {
            var type = message.GetType();

            if (_handlers.TryGetValue(type, out var handlers))
            {
                var args = new object[] { message };

                foreach (var handler in handlers)
                {
                    handler.Method.Invoke(handler.Instance, args);
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the message dispatcher.
        /// </summary>
        public void Dispose()
        {
            _queue.CompleteAdding();
            GC.SuppressFinalize(this);
        }
    }
}
