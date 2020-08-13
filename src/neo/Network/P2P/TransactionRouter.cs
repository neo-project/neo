using Akka.Actor;
using Akka.Routing;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.Network.P2P
{
    public class TransactionRouter : UntypedActor
    {
        private readonly NeoSystem system;

        public TransactionRouter(NeoSystem system)
        {
            this.system = system;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Transaction tx:
                    if (tx.VerifyStateIndependent() == VerifyResult.Succeed)
                        OnTransactionReceived(tx);
                    break;
            }
        }

        private void OnTransactionReceived(Transaction tx)
        {
            system.TaskManager.Tell(tx);
            system.Consensus?.Tell(tx);
            system.Blockchain.Tell(tx, ActorRefs.NoSender);
        }

        internal static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new TransactionRouter(system)).WithRouter(new SmallestMailboxPool(20));
        }
    }
}
