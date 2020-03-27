using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Oracle;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class OracleResult : IVerifiable
    {
        public UInt256 TransactionHash;
        public OracleResponse Response;
        public ECPoint OraclePub;
        public Witness Witness;

        public int Size =>
            UInt256.Length +    //Transaction Hash
            Response.Size +     //Timestamp
            OraclePub.Size +    //Oracle Public key
            Witness.Size;       //Oracle Validator Signature

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

        public virtual void Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            if (witnesses.Length != 1) throw new FormatException();
            Witness = witnesses[0];
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            TransactionHash = reader.ReadSerializable<UInt256>();
            Response = reader.ReadSerializable<OracleResponse>();
            OraclePub = reader.ReadSerializable<ECPoint>();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
        {
            return new[] { Contract.CreateSignatureRedeemScript(OraclePub).ToScriptHash() };
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(new Witness[] { Witness });
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(TransactionHash);
            writer.Write(Response);
            writer.Write(OraclePub);
        }

        public bool Verify(StoreView snapshot)
        {
            var pubs = NativeContract.Oracle.GetOracleValidators(snapshot);

            if (!pubs.Any(u => u.Equals(OraclePub)))
            {
                return false;
            }
            return this.VerifyWitnesses(snapshot, 0_02000000);
        }
    }
}
