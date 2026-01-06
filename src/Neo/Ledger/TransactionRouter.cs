// Copyright (C) 2015-2026 The Neo Project.
//
// TransactionRouter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Routing;
using Neo.Network.P2P.Payloads;

namespace Neo.Ledger;

internal class TransactionRouter(NeoSystem system) : UntypedActor
{
    public record Preverify(Transaction Transaction, bool Relay);
    public record PreverifyCompleted(Transaction Transaction, bool Relay, VerifyResult Result);

    private readonly NeoSystem _system = system;

    protected override void OnReceive(object message)
    {
        if (message is not Preverify preverify) return;
        var send = new PreverifyCompleted(preverify.Transaction, preverify.Relay,
                preverify.Transaction.VerifyStateIndependent(_system.Settings));
        _system.Blockchain.Tell(send, Sender);
    }

    internal static Props Props(NeoSystem system)
    {
        return Akka.Actor.Props.Create(() => new TransactionRouter(system)).WithRouter(new SmallestMailboxPool(Environment.ProcessorCount));
    }
}
