
using Neo.Persistence;
using System.Text;

namespace Neo.Trie.MPT
{
    public class MPTDatabase
    {
        private ISnapshot store;

        public static readonly byte TABLE = 0x4D;

        public static readonly byte[] Prefix = Encoding.ASCII.GetBytes("MPT");

        private byte[] StoreKey(byte[] hash)
        {
            return Prefix.Concat(hash);
        }

        public MPTDatabase(ISnapshot store)
        {
            this.store = store;
        }

        public MPTNode Node(byte[] hash)
        {
            var data = store.TryGet(TABLE, StoreKey(hash));
            var n = MPTNode.Decode(data);
            return n;
        }

        public void Delete(byte[] hash)
        {
            store.Delete(TABLE, StoreKey(hash));
        }

        public void Put(MPTNode node)
        {
            store.Put(TABLE, StoreKey(node.GetHash()), node.Encode());
        }

        public void PutRoot(byte[] root)
        {
            store.Put(TABLE, StoreKey(Encoding.ASCII.GetBytes("mpt_root")), root);
        }

        public byte[] GetRoot()
        {
            return store.TryGet(TABLE, StoreKey(Encoding.ASCII.GetBytes("mpt_root")));
        }
    }
}