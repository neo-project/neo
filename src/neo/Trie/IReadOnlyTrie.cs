using System.Collections.Generic;

namespace Neo.Trie
{
    public interface IReadOnlyTrie
    {
        bool TryGet(byte[] path, out byte[] value);

        byte[] GetRoot();

        Dictionary<byte[], byte[]> GetProof(byte[] path);

        bool VerifyProof(byte[] path, Dictionary<byte[], byte[]> proof);
    }
}
