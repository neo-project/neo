using System;
using System.Linq;

namespace Neo.Ledger.MPT.Operation
{
    /// <summary>
    /// MPT validate operation.
    /// </summary>
    internal class MPTValidate
    {
        /// <summary>
        /// Delegate to get data from the database.
        /// </summary>
        private readonly Func<byte[], MerklePatriciaNode> _getDb;

        /// <summary>
        /// Delegate to check if the database contains the key.
        /// </summary>
        private readonly Func<byte[], bool> _containsKeyDb;

        /// <summary>
        /// MPT validate operation. 
        /// </summary>
        /// <param name="_getDb">Delegate to get data from the database.</param>
        /// <param name="_containsKeyDb">Delegate to check if the database contains the key.</param>
        internal MPTValidate(Func<byte[], MerklePatriciaNode> _getDb, Func<byte[], bool> _containsKeyDb)
        {
            this._getDb = _getDb;
            this._containsKeyDb = _containsKeyDb;
        }

        /// <summary>
        /// Check if the MPT is valid.
        /// </summary>
        /// <param name="key">Root key.</param>
        /// <returns>true case the MPT is valid.</returns>
        public bool Validate(byte[] key) => key == null
                                            || (_containsKeyDb(key) && Validate(key, _getDb(key)));

        private bool Validate(byte[] nodeHash, MerklePatriciaNode node)
        {
            while (true)
            {
                if (!nodeHash.SequenceEqual(node.Hash()))
                {
                    return false;
                }

                if (node.IsExtension)
                {
                    nodeHash = node.Next;
                    node = _getDb(node.Next);
                    continue;
                }

                if (node.IsLeaf)
                {
                    return true;
                }

                foreach (var subNodeHash in node)
                {
                    if (subNodeHash != null && !Validate(subNodeHash, _getDb(subNodeHash)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}