using System.Collections.Generic;

namespace Neo.Persistence
{
    public interface IReadOnlyStore
    {
        IEnumerable<(byte[] Key, byte[] Value)> Find(byte table, byte[] prefix);
        byte[] TryGet(byte table, byte[] key);
    }
}
