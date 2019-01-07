namespace Neo.Consensus
{
    internal class PrepareResponse : ConsensusMessage
    {
        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
        }
    }
}
