namespace Neo.SmartContract
{
    /// <summary>
    /// The storage context used to read and write data in smart contracts.
    /// </summary>
    public class StorageContext
    {
        /// <summary>
        /// The id of the contract that owns the context.
        /// </summary>
        public int Id;

        /// <summary>
        /// Indicates whether the context is read-only.
        /// </summary>
        public bool IsReadOnly;
    }
}
