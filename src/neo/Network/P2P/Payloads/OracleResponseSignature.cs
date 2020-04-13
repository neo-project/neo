using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.Network.P2P.Payloads
{
    public class OracleResponseSignature : ISerializable
    {
        private const byte ResponseSignatureType = 0x01;

        private UInt256 _transactionRequestHash;
        public UInt256 TransactionRequestHash
        {
            get => _transactionRequestHash;
            set { _transactionRequestHash = value; _hash = null; _size = 0; }
        }

        private UInt256 _oracleExecutionCacheHash;
        public UInt256 OracleExecutionCacheHash
        {
            get => _oracleExecutionCacheHash;
            set { _oracleExecutionCacheHash = value; _hash = null; _size = 0; }
        }

        /// <summary>
        /// Signature for the oracle response tx for this public key
        /// </summary>
        private byte[] _signature;
        public byte[] Signature
        {
            get => _signature;
            set
            {
                if (value.Length != 64) throw new ArgumentException();
                _signature = value;
                _hash = null;
                _size = 0;
            }
        }

        private int _size;
        public int Size
        {
            get
            {
                if (_size == 0)
                {
                    _size = sizeof(byte) +  //Type
                        UInt256.Length +    //Transaction Hash
                        UInt256.Length +    //OracleExecutionCache Hash
                        Signature.Length;   //Oracle Validator Signature
                }
                return _size;
            }
        }

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Hash256(this.ToArray()));
                }
                return _hash;
            }
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != ResponseSignatureType) throw new FormatException();
            DeserializeWithoutType(reader);
        }

        private void DeserializeWithoutType(BinaryReader reader)
        {
            TransactionRequestHash = reader.ReadSerializable<UInt256>();
            OracleExecutionCacheHash = reader.ReadSerializable<UInt256>();
            Signature = reader.ReadFixedBytes(64);
        }

        public static OracleResponseSignature DeserializeFrom(byte[] data)
        {
            switch (data[0])
            {
                case ResponseSignatureType:
                    {
                        using BinaryReader reader = new BinaryReader(new MemoryStream(data, 1, data.Length - 1), Encoding.UTF8, false);

                        var ret = new OracleResponseSignature();
                        ret.DeserializeWithoutType(reader);
                        return ret;
                    }
                default: throw new FormatException();
            }
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(ResponseSignatureType);
            writer.Write(TransactionRequestHash);
            writer.Write(OracleExecutionCacheHash);
            writer.Write(Signature);
        }
    }
}
