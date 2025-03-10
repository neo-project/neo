// Copyright (C) 2015-2025 The Neo Project.
//
// PriorityMessageQueue.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Akka.Actor;
using Akka.Dispatch;
using Akka.Dispatch.MessageQueues;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Neo.IO.Actors
{
    internal class PriorityMessageQueue
        (Func<object, IEnumerable, bool> dropper, Func<object, bool> priority_generator) : IMessageQueue, IUnboundedMessageQueueSemantics
    {
        private readonly ConcurrentQueue<Envelope> _high = new();
        private readonly ConcurrentQueue<Envelope> _low = new();
        private readonly Func<object, IEnumerable, bool> _dropper = dropper;
        private readonly Func<object, bool> _priority_generator = priority_generator;
        private int _idle = 1;

        public bool HasMessages => !_high.IsEmpty || !_low.IsEmpty;
        public int Count => _high.Count + _low.Count;

        public void CleanUp(IActorRef owner, IMessageQueue deadletters)
        {
        }

        public void Enqueue(IActorRef receiver, Envelope envelope)
        {
            Interlocked.Increment(ref _idle);
            if (envelope.Message is Idle) return;
            if (_dropper(envelope.Message, _high.Concat(_low).Select(p => p.Message)))
                return;
            var queue = _priority_generator(envelope.Message) ? _high : _low;
            queue.Enqueue(envelope);
        }

        public bool TryDequeue(out Envelope envelope)
        {
            if (_high.TryDequeue(out envelope)) return true;
            if (_low.TryDequeue(out envelope)) return true;
            if (Interlocked.Exchange(ref _idle, 0) > 0)
            {
                envelope = new Envelope(Idle.Instance, ActorRefs.NoSender);
                return true;
            }
            return false;
        }
    }
}

#nullable disable
