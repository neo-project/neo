using Neo.IO;
using Neo.IO.Caching;

namespace Neo.Ledger.MPT
{
    /// <inheritdoc />
    /// <summary>
    /// The MerklePatriciaDataCache data structure delegates the calls to the DataCache,
    /// so there is no need to retrieve all the MPT from the database.
    /// </summary>
    public class MerklePatriciaDataCache : MerklePatricia
    {
        private readonly DataCache<MPTKey, MerklePatriciaNode> db;
        private byte[] _rootHash;

        public MerklePatriciaDataCache(DataCache<MPTKey, MerklePatriciaNode> db, byte[] rootHash)
        {
            this.db = db;
            _rootHash = rootHash;
            if (_rootHash != null && _rootHash.Length == 0)
            {
                _rootHash = null;
            }
        }

        /// <inheritdoc />
        protected override MerklePatriciaNode GetDb(byte[] hash) => db.TryGet(hash.AsSerializable<MPTKey>());

        /// <inheritdoc />
        protected override bool RemoveDb(byte[] hash)
        {
            db.Delete(hash.AsSerializable<MPTKey>());
            return true;
        }

        /// <inheritdoc />
        protected override MerklePatriciaNode SetDb(byte[] hash, MerklePatriciaNode node)
        {
            db.Add(hash.AsSerializable<MPTKey>(), node);
            return node;
        }

        /// <inheritdoc />
        protected override bool ContainsKeyDb(byte[] key) => db.TryGet(key.AsSerializable<MPTKey>()) != null;

        /// <inheritdoc />
        protected override byte[] GetRoot() => _rootHash;

        /// <inheritdoc />
        protected override void SetRoot(byte[] root) => _rootHash = root;
    }
}