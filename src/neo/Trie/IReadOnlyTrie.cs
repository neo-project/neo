using System.Collections.Generic;

namespace Neo.Trie
{
    public interface IReadOnlyTrie
    {
        bool TryGet(byte[] path, out byte[] value);

        byte[] GetRoot();

        HashSet<byte[]> GetProof(byte[] path);

        bool VerifyProof(byte[] path, HashSet<byte[]> proof);
    }
}
