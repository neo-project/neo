using Neo.IO.Caching;

namespace Neo.Consensus
{
    public enum ConsensusMessageType : byte
    {
        [ReflectionCache(typeof(ChangeView))]
        ChangeView = 0x00,

        [ReflectionCache(typeof(PrepareRequest))]
        PrepareRequest = 0x20,
        [ReflectionCache(typeof(PrepareResponse))]
        PrepareResponse = 0x21,
        [ReflectionCache(typeof(Commit))]
        Commit = 0x30,

        [ReflectionCache(typeof(RecoveryRequest))]
        RecoveryRequest = 0x40,
        [ReflectionCache(typeof(RecoveryMessage))]
        RecoveryMessage = 0x41,
    }
}
