
namespace Neo.Trie
{
    public interface IKVReadOnlyStore
    {
        byte[] Get(byte[] key);
    }
}
