using Neo.IO;
using System.IO;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Persistence;
using System;
using System.Linq;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace Neo.Network.P2P.Payloads
{
    public class OraclePayload : IVerifiable
    {
        private byte[] _data;
        public byte[] Data
        {
            get => _data;
            set { _data = value; _hash = null; _size = 0; }
        }

        private ECPoint _oraclePub;
        public ECPoint OraclePub
        {
            get => _oraclePub;
            set { _oraclePub = value; _hash = null; _size = 0; }
        }

        private Witness _witness;
        public Witness Witness
        {
            get => _witness;
            set { _witness = value; _hash = null; _size = 0; }
        }

        private int _size;
        public int Size
        {
            get
            {
                if (_size == 0)
                {
                    _size = sizeof(byte) +  //Type
                        Data.GetVarSize() + //Data
                        OraclePub.Size +    //Oracle Public key
                        Witness.Size;       //Witness
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
                    _hash = new UInt256(Crypto.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Witness = value[0];
            }
        }

        private OracleResponseSignature _deserializedOracleSignature = null;
        public OracleResponseSignature OracleSignature
        {
            get
            {
                if (_deserializedOracleSignature is null)
                    _deserializedOracleSignature = OracleResponseSignature.DeserializeFrom(Data);
                return _deserializedOracleSignature;
            }
            internal set
            {
                if (!ReferenceEquals(_deserializedOracleSignature, value))
                {
                    _deserializedOracleSignature = value;
                    Data = value?.ToArray();
                }
            }
        }

        public OracleResponseSignature GetDeserializedOracleSignature()
        //public T GetDeserializedOracleSignature<T>() where T : OracleResponseSignature
        {
            return OracleSignature;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witness = reader.ReadSerializable<Witness>();
        }
        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Data = reader.ReadVarBytes(Transaction.MaxTransactionSize);
            OraclePub = reader.ReadSerializable<ECPoint>();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(new Witness[] { Witness });
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Data);
            writer.Write(OraclePub);
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
        {
            //ECPoint[] validators = NativeContract.Oracle.GetOracleValidators(snapshot);
            //if (validators.Length <= OracleValidatorIndex)
            //    throw new InvalidOperationException();
            //return new[] { Contract.CreateSignatureRedeemScript(validators[OracleValidatorIndex]).ToScriptHash() };
            return new[] { Contract.CreateSignatureRedeemScript(OraclePub).ToScriptHash() };
        }

        public bool Verify(StoreView snapshot)
        {
            ECPoint[] validators = NativeContract.Oracle.GetOracleValidators(snapshot);
            if (!validators.Any(u => u.Equals(OraclePub)))
                return false;
            return this.VerifyWitnesses(snapshot, 0_02000000);
        }
    }
}
