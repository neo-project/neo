using Neo.Persistence;
using System.Text;

namespace Neo.Trie.MPT
{
    public class MPTDatabase
    {
        private ISnapshot store;
        public static readonly byte TABLE = 0x4D;
        public static readonly byte[] ROOT_KEY = Encoding.ASCII.GetBytes("MPT_ROOT");

        public MPTDatabase(ISnapshot store)
        {
            this.store = store;
        }

        public MPTNode Node(byte[] hash)
        {
            var data = store.TryGet(TABLE, hash);
            var n = MPTNode.Decode(data);
            return n;
        }

        public void Delete(byte[] hash)
        {
            store.Delete(TABLE, hash);
        }

        public void Put(MPTNode node)
        {
            store.Put(TABLE, node.GetHash(), node.Encode());
        }

        public void PutRoot(byte[] root)
        {
            store.Put(TABLE, ROOT_KEY, root);
        }

        public byte[] GetRoot()
        {
            return store.TryGet(TABLE, ROOT_KEY);
        }
    }
}
