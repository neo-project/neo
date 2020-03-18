
using System.Collections.Generic;

namespace Neo.Trie
{
    public interface ITrie : IReadOnlyTrie
    {
        bool Put(byte[] path, byte[] value);

        bool TryDelete(byte[] path);
    }
}
