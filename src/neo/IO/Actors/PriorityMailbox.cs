using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.MessageQueues;

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
            if (this is IDropeable dropeable) return new PriorityMessageQueue(dropeable.ShallDrop, IsHighPriority);
            return new PriorityMessageQueue(null, IsHighPriority);
        }

        internal protected virtual bool IsHighPriority(object message) => false;
    }
}
