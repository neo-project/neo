using System.Collections.Generic;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface IReadOnlyStore
    {
        IEnumerable<(byte[] Key, byte[] Value)> Seek(byte table, byte[] key);
        byte[] TryGet(byte table, byte[] key);
    }
}
