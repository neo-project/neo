using Neo.Cryptography;
using Neo.Cryptography.ECC;
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

        public UInt256 TransactionRequestHash;
        public UInt256 OracleExecutionCacheHash;
        public ECPoint OraclePub;
        // Signature for the oracle response tx for this public key
        public byte[] Signature;

        public int Size =>
            sizeof(byte) +      //Type=0x01=OracleResponseSignature
            UInt256.Length +    //Transaction Hash
            UInt256.Length +    //OracleExecutionCache Hash
            OraclePub.Size +    //Oracle Public key
            Signature.GetVarSize();       //Oracle Validator Signature

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
            OraclePub = reader.ReadSerializable<ECPoint>();
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
            writer.Write(OraclePub);
            writer.Write(Signature);
        }
    }
}
