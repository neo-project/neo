using System;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods for reading, writing from/to database. Developers should implement this interface to provide new storage engines for NEO.
    /// </summary>
    public interface IStore : IDisposable, IReadOnlyStore
    {
        /// <summary>
        /// Deletes an entry from the database.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        void Delete(byte[] key);

        /// <summary>
        /// Creates a snapshot of the database.
        /// </summary>
        /// <returns>A snapshot of the database.</returns>
        ISnapshot GetSnapshot();

        /// <summary>
        /// Puts an entry to the database.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        void Put(byte[] key, byte[] value);

        /// <summary>
        /// Puts an entry to the database synchronously.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        void PutSync(byte[] key, byte[] value) => Put(key, value);
    }
}
