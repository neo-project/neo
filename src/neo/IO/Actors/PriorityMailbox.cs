// Copyright (C) 2015-2021 NEO GLOBAL DEVELOPMENT.
// 
// The Neo project is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            return new PriorityMessageQueue(ShallDrop, IsHighPriority);
        }

        internal protected virtual bool IsHighPriority(object message) => false;
        internal protected virtual bool ShallDrop(object message, IEnumerable queue) => false;
    }
}
