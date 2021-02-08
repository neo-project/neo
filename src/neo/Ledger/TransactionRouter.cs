using Akka.Actor;
using Akka.Routing;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Ledger
{
    internal class TransactionRouter : UntypedActor
    {
        private readonly NeoSystem system;

        public TransactionRouter(NeoSystem system)
        {
            this.system = system;
        }

        protected override void OnReceive(object message)
        {
            if (message is not Transaction tx) return;
            system.Blockchain.Tell(new Blockchain.PreverifyCompleted
            {
                Transaction = tx,
                Result = tx.VerifyStateIndependent(system.Settings)
            }, Sender);
        }

        internal static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new TransactionRouter(system)).WithRouter(new SmallestMailboxPool(Environment.ProcessorCount));
        }
    }
}
