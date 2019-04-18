namespace Neo.Consensus
{
    public class RecoveryRequest : ConsensusMessage
    {
        public RecoveryRequest() : base(ConsensusMessageType.RecoveryRequest) { }
    }
}
