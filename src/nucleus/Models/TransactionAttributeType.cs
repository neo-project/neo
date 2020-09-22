namespace Neo.Models
{
    public enum TransactionAttributeType : byte
    {
        // [ReflectionCache(typeof(HighPriorityAttribute))]
        HighPriority = 0x01,
        // [ReflectionCache(typeof(OracleResponse))]
        OracleResponse = 0x11
    }
}
