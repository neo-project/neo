
namespace Neo.Trie
{
    public interface IKVStore : IKVReadOnlyStore
    {
        void Put(byte[] key, byte[] value);
    }
}
