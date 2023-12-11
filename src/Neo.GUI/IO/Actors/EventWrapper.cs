// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using System;

namespace Neo.IO.Actors
{
    internal class EventWrapper<T> : UntypedActor
    {
        private readonly Action<T> callback;

        public EventWrapper(Action<T> callback)
        {
            this.callback = callback;
            Context.System.EventStream.Subscribe(Self, typeof(T));
        }

        protected override void OnReceive(object message)
        {
            if (message is T obj) callback(obj);
        }

        protected override void PostStop()
        {
            Context.System.EventStream.Unsubscribe(Self);
            base.PostStop();
        }

        public static Props Props(Action<T> callback)
        {
            return Akka.Actor.Props.Create(() => new EventWrapper<T>(callback));
        }
    }
}
