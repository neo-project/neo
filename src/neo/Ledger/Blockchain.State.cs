using Akka.Actor;
using Neo.Network.P2P.Payloads;

namespace Neo.Ledger
{
    public sealed partial class Blockchain : UntypedActor
    {
        public StateRoot LatestValidatorsStateRoot => new StateRoot();
        public long StateHeight => -1;
    }
}