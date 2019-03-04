using Neo.IO.Caching;

namespace Neo.Consensus
{
    internal enum ConsensusMessageType : byte
    {
        [ReflectionCache(typeof(ChangeView))]
        ChangeView = 0x00,

        [ReflectionCache(typeof(PrepareRequest))]
        PrepareRequest = 0x20,
        [ReflectionCache(typeof(PrepareResponse))]
        PrepareResponse = 0x21,
        [ReflectionCache(typeof(Commit))]
        Commit = 0x30,

        [ReflectionCache(typeof(RecoveryMessage))]
        RecoveryMessage = 0x41,
    }
}
