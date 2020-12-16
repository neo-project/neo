using Akka.Actor;
using Akka.Routing;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Ledger
{
    internal class TransactionRouter : UntypedActor
    {
        private readonly IActorRef blockchain;

        public TransactionRouter(NeoSystem system)
        {
            this.blockchain = system.Blockchain;
        }

        protected override void OnReceive(object message)
        {
            if (!(message is Transaction tx)) return;
            blockchain.Tell(new Blockchain.PreverifyCompleted
            {
                Transaction = tx,
                Result = tx.VerifyStateIndependent(),
                Relay = true
            }, Sender);
        }

        internal static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new TransactionRouter(system)).WithRouter(new SmallestMailboxPool(Environment.ProcessorCount));
        }
    }
}
