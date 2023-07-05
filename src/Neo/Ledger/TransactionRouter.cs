// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Routing;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Ledger
{
    internal class TransactionRouter : UntypedActor
    {
        public record Preverify(Transaction Transaction, bool Relay);
        public record PreverifyCompleted(Transaction Transaction, bool Relay, VerifyResult Result);

        private readonly NeoSystem system;

        public TransactionRouter(NeoSystem system)
        {
            this.system = system;
        }

        protected override void OnReceive(object message)
        {
            if (message is not Preverify preverify) return;
            system.Blockchain.Tell(new PreverifyCompleted(preverify.Transaction, preverify.Relay, preverify.Transaction.VerifyStateIndependent(system.Settings)), Sender);
        }

        internal static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new TransactionRouter(system)).WithRouter(new SmallestMailboxPool(Environment.ProcessorCount));
        }
    }
}
