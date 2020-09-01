using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.MessageQueues;
using System.Collections;

namespace Neo.IO.Actors
{
    internal abstract class PriorityMailbox : MailboxType, IProducesMessageQueue<PriorityMessageQueue>
    {
        public PriorityMailbox(Akka.Actor.Settings settings, Config config)
            : base(settings, config)
        {
        }

        public override IMessageQueue Create(IActorRef owner, ActorSystem system)
        {
            return new PriorityMessageQueue(ShallDrop, IsHighPriority, AfterDequeue);
        }

        internal protected virtual bool IsHighPriority(object message) => false;
        internal protected virtual bool ShallDrop(object message, IEnumerable queue) => false;
        internal protected virtual void AfterDequeue(object message) { }
    }
}
