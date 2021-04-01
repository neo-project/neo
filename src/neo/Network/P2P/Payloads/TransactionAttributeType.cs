using Neo.IO.Caching;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents the type of a <see cref="TransactionAttribute"/>.
    /// </summary>
    public enum TransactionAttributeType : byte
    {
        /// <summary>
        /// Indicates that the transaction is of high priority.
        /// </summary>
        [ReflectionCache(typeof(HighPriorityAttribute))]
        HighPriority = 0x01,

        /// <summary>
        /// Indicates that the transaction is an oracle response.
        /// </summary>
        [ReflectionCache(typeof(OracleResponse))]
        OracleResponse = 0x11,
        [ReflectionCache(typeof(NotValidBefore))]
        NotValidBeforeT = 0xe0,
        [ReflectionCache(typeof(Conflicts))]
        ConflictsT = 0xe1,
        [ReflectionCache(typeof(NotaryAssisted))]
        NotaryAssistedT = 0xe2
    }
}
