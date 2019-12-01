using System;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods for reading, writing from/to database. Developers should implement this interface to provide new storage engines for NEO.
    /// </summary>
    public interface IStore : IDisposable, IReadOnlyStore
    {
        void Delete(byte table, byte[] key);
        ISnapshot GetSnapshot();
        void Put(byte table, byte[] key, byte[] value);
        void PutSync(byte table, byte[] key, byte[] value)
        {
            Put(table, key, value);
        }
    }
}
