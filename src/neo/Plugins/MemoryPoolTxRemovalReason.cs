namespace Neo.Plugins
{
    /// <summary>
    /// The reason a transaction was removed.
    /// </summary>
    public enum MemoryPoolTxRemovalReason : byte
    {
        /// <summary>
        /// The transaction was ejected since it was the lowest priority transaction and the memory pool capacity was exceeded.
        /// </summary>
        CapacityExceeded,

        /// <summary>
        /// The transaction was ejected due to failing re-validation after a block was persisted.
        /// </summary>
        NoLongerValid,
    }
}
