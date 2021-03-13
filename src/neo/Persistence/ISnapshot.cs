using System;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods for reading, writing, and committing from/to snapshot.
    /// </summary>
    public interface ISnapshot : IDisposable, IReadOnlyStore
    {
        /// <summary>
        /// Commits all changes in the snapshot to the database.
        /// </summary>
        void Commit();

        /// <summary>
        /// Deletes an entry from the snapshot.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        void Delete(byte[] key);

        /// <summary>
        /// Puts an entry to the snapshot.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        void Put(byte[] key, byte[] value);
    }
}
