using System.Collections.Generic;

namespace Neo.Trie
{
    public interface IReadOnlyTrie
    {
        bool TryGet(byte[] path, out byte[] value);

        UInt256 GetRoot();

        bool GetProof(byte[] path, out HashSet<byte[]> proof);
    }
}
