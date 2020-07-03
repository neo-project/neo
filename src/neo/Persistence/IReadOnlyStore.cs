using System.Collections.Generic;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface IReadOnlyStore
    {
        IEnumerable<(byte[] Key, byte[] Value)> Find(byte table, byte[] prefix);
        byte[] TryGet(byte table, byte[] key);
    }
}
