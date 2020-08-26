
namespace Neo.Trie
{
    public interface IKVStore
    {
        byte[] Get(byte[] key);
        void Put(byte[] key, byte[] value);

        void Delete(byte[] key);
    }
}
