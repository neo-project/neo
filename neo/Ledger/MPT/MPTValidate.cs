using System;
using System.Linq;

namespace Neo.Ledger.MPT
{
    public class MPTValidate
    {
        private readonly Func<byte[], MerklePatriciaNode> _getDb;
        private readonly Func<byte[], bool> _containsKeyDb;

        internal MPTValidate(Func<byte[], MerklePatriciaNode> _getDb, Func<byte[], bool> _containsKeyDb)
        {
            this._getDb = _getDb;
            this._containsKeyDb = _containsKeyDb;
        }

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