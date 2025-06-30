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

namespace Neo.IO
{
    public abstract class MessageReceiver<T>(MessageRelayer relayer)
        : MessageReceiver(relayer, typeof(T))
    { }

    public abstract class MessageReceiver
    {
        /// <summary>
        /// Accepted message types that this receiver can handle.
        /// </summary>
        public Type[] AcceptedMessages { get; }

        /// <summary>
        /// Relayer used to send messages to this receiver.
        /// </summary>
        public MessageRelayer Relayer { get; }

        /// <summary>
        /// Constructs a new message receiver.
        /// </summary>
        /// <param name="relayer">Relayer</param>
        /// <param name="acceptedTypes">Accepted types</param>
        public MessageReceiver(MessageRelayer relayer, params Type[] acceptedTypes)
        {
            Relayer = relayer;
            AcceptedMessages = acceptedTypes;
            Relayer.Subscribe(this, acceptedTypes);
        }

        public abstract void OnReceive(object message);

        #region Redrected Methods

        #region Single Entry

        public void Tell(object message) => Relayer.Tell(message);
        public void TellPriorty(object message) => Relayer.TellPriorty(message);

        #endregion

        #region Multiple Entries

        public void Tell(params object[] messages) => Relayer.Tell(messages);
        public void TellPriority(params object[] messages) => Relayer.TellPriority(messages);

        #endregion

        #endregion
    }
}
