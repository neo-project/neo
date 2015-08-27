using AntShares.Core;
using AntShares.Cryptography;
using AntShares.IO;
using System.IO;

namespace AntShares.Network
{
    public abstract class Inventory : ISerializable
    {
        private UInt256 _hash = null;

        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = GetHash();
                }
                return _hash;
            }
        }

        public abstract InventoryType InventoryType { get; }

        public abstract void Deserialize(BinaryReader reader);

        protected virtual UInt256 GetHash()
        {
            return new UInt256(this.ToArray().Sha256().Sha256());
        }

        public abstract void Serialize(BinaryWriter writer);

        public abstract VerificationResult Verify();
    }
}
