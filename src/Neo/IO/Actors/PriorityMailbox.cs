// Copyright (C) 2015-2025 The Neo Project.
//
// PriorityMailbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.MessageQueues;
using System.Collections;

namespace Neo.IO.Actors
{
    internal abstract class PriorityMailbox
        (Settings settings, Config config) : MailboxType(settings, config), IProducesMessageQueue<PriorityMessageQueue>
    {
        public override IMessageQueue Create(IActorRef owner, ActorSystem system) =>
            new PriorityMessageQueue(ShallDrop, IsHighPriority);

        internal protected virtual bool IsHighPriority(object message) => false;
        internal protected virtual bool ShallDrop(object message, IEnumerable queue) => false;
    }
}

#nullable disable
