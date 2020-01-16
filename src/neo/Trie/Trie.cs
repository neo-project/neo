
namespace Neo.Trie
{
    public interface ITrie
    {
        bool TryGet(byte[] path, out byte[] value);

        bool TryPut(byte[] path, byte[] value);

        bool TryDelete(byte[] path);

        byte[] GetRoot();

        bool Prove(byte[] key, byte[] proof);

        byte[] GetProof(byte[] Key, byte [] value);
    }
}