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
    internal class PriorityMessageQueue : IMessageQueue, IUnboundedMessageQueueSemantics
    {
        private readonly ConcurrentQueue<Envelope> high = new ConcurrentQueue<Envelope>();
        private readonly ConcurrentQueue<Envelope> low = new ConcurrentQueue<Envelope>();
        private readonly Func<object, IEnumerable, bool> dropper;
        private readonly Func<object, bool> priority_generator;
        private int idle = 1;

        public bool HasMessages => !high.IsEmpty || !low.IsEmpty;
        public int Count => high.Count + low.Count;

        public PriorityMessageQueue(Func<object, IEnumerable, bool> dropper, Func<object, bool> priority_generator)
        {
            this.dropper = dropper;
            this.priority_generator = priority_generator;
        }

        public void CleanUp(IActorRef owner, IMessageQueue deadletters)
        {
        }

        public void Enqueue(IActorRef receiver, Envelope envelope)
        {
            Interlocked.Increment(ref idle);
            if (envelope.Message is Idle) return;
            if (dropper(envelope.Message, high.Concat(low).Select(p => p.Message)))
                return;
            ConcurrentQueue<Envelope> queue = priority_generator(envelope.Message) ? high : low;
            queue.Enqueue(envelope);
        }

        public bool TryDequeue(out Envelope envelope)
        {
            if (high.TryDequeue(out envelope)) return true;
            if (low.TryDequeue(out envelope)) return true;
            if (Interlocked.Exchange(ref idle, 0) > 0)
            {
                envelope = new Envelope(Idle.Instance, ActorRefs.NoSender);
                return true;
            }
            return false;
        }
    }
}
