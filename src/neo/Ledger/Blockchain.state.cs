using Akka.Actor;

namespace Neo.Ledger
{
    public sealed partial class Blockchain : UntypedActor
    {
        public UInt256 GetStateRoot(uint index)
        {
            return UInt256.Zero;
        }
    }
}
