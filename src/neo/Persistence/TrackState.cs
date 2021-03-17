namespace Neo.Persistence
{
    /// <summary>
    /// Represents the state of a cached entry.
    /// </summary>
    public enum TrackState : byte
    {
        /// <summary>
        /// Indicates that the entry has been loaded from the underlying storage, but has not been modified.
        /// </summary>
        None,

        /// <summary>
        /// Indicates that this is a newly added record.
        /// </summary>
        Added,

        /// <summary>
        /// Indicates that the entry has been loaded from the underlying storage, and has been modified.
        /// </summary>
        Changed,

        /// <summary>
        /// Indicates that the entry should be deleted from the underlying storage when committing.
        /// </summary>
        Deleted
    }
}
