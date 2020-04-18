using Neo.IO;
using System;
using System.IO;
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
            set { _transactionRequestHash = value; _size = 0; }
        }

        private UInt256 _transactionResponseHash;
        public UInt256 TransactionResponseHash
        {
            get => _transactionResponseHash;
            set { _transactionResponseHash = value; _size = 0; }
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

        public virtual void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != ResponseSignatureType) throw new FormatException();
            DeserializeWithoutType(reader);
        }

        private void DeserializeWithoutType(BinaryReader reader)
        {
            TransactionRequestHash = reader.ReadSerializable<UInt256>();
            TransactionResponseHash = reader.ReadSerializable<UInt256>();
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
            writer.Write(TransactionResponseHash);
            writer.Write(Signature);
        }
    }
}
