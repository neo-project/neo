
namespace Neo.Trie
{
    public interface ITrie : IReadOnlyTrie
    {
        bool Put(byte[] key, byte[] value);

        bool TryDelete(byte[] key);
    }
}
