using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography;
using System;
using System.IO;

namespace AntShares.Network
{
    public abstract class Inventory : ISignable
    {
        [NonSerialized]
        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(GetHashData().Sha256().Sha256());
                }
                return _hash;
            }
        }

        public abstract Script[] Scripts { get; set; }

        public abstract InventoryType InventoryType { get; }

        public abstract void Deserialize(BinaryReader reader);

        public abstract void DeserializeUnsigned(BinaryReader reader);

        private byte[] GetHashData()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public abstract UInt160[] GetScriptHashesForVerifying();

        public abstract void Serialize(BinaryWriter writer);

        public abstract void SerializeUnsigned(BinaryWriter writer);

        public abstract bool Verify();
    }
}
