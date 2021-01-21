using System;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods for reading, writing, and committing from/to snapshot.
    /// </summary>
    public interface ISnapshot : IDisposable, IReadOnlyStore
    {
        void Commit();
        void Delete(byte[] key);
        void Put(byte[] key, byte[] value);
    }
}
