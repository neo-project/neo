using Akka.Actor;
using Akka.Routing;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Ledger
{
    public sealed class TransactionRouter : UntypedActor
    {
        private readonly NeoSystem system;
        private readonly Blockchain chain;

        public TransactionRouter(NeoSystem system, Blockchain bc)
        {
            this.system = system;
            this.chain = bc;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Transaction tx:
                    OnTransaction(tx, true);
                    break;
                case Transaction[] transactions:
                    // This message comes from a mempool's revalidation, already relayed
                    foreach (var tx in transactions) OnTransaction(tx, false);
                    break;
            }
        }

        private void OnTransaction(Transaction tx, bool relay)
        {
            VerifyResult res = chain.MemPool.TryAdd(tx);
            if (relay && res == VerifyResult.Succeed)
                system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = tx });
            SendRelayResult(tx, res);
        }

        private void SendRelayResult(IInventory inventory, VerifyResult result)
        {
            Blockchain.RelayResult rr = new Blockchain.RelayResult
            {
                Inventory = inventory,
                Result = result
            };
            Sender.Tell(rr);
            Context.System.EventStream.Publish(rr);
        }

        public static Props Props(NeoSystem system, Blockchain bc)
        {
            return Akka.Actor.Props.Create(() => new TransactionRouter(system, bc)).WithRouter(new SmallestMailboxPool(Environment.ProcessorCount));
        }
    }
}
