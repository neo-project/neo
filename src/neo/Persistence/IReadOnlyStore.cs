using System.Collections.Generic;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface IReadOnlyStore
    {
        IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] key, SeekDirection direction);
        byte[] TryGet(byte[] key);
        bool Contains(byte[] key);
    }
}
