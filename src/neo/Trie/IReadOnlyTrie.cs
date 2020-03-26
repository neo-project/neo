using System.Collections.Generic;

namespace Neo.Trie
{
    public interface IReadOnlyTrie
    {
        bool TryGet(byte[] key, out byte[] value);

        UInt256 GetRoot();

        bool GetProof(byte[] key, out HashSet<byte[]> set);
    }
}
