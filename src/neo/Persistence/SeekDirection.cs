namespace Neo.Persistence
{
    /// <summary>
    /// Represents the direction when searching from the database.
    /// </summary>
    public enum SeekDirection : sbyte
    {
        /// <summary>
        /// Indicates that the search should be performed in ascending order.
        /// </summary>
        Forward = 1,

        /// <summary>
        /// Indicates that the search should be performed in descending order.
        /// </summary>
        Backward = -1
    }
}
