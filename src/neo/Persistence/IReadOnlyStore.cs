using Neo.IO.Caching;
using System.Collections.Generic;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface IReadOnlyStore
    {
        IEnumerable<(byte[] Key, byte[] Value)> Seek(byte table, byte[] key, SeekDirection direction);
        byte[] TryGet(byte table, byte[] key);
        bool Contains(byte table, byte[] key);
    }
}
