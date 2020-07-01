using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class OraclePayload : IInventory
    {
        private const long MaxWitnessGas = 0_02000000;

        public ECPoint OraclePub;
        public UInt256 RequestTxHash;
        public byte[] ResponseTxSignature;

        public Witness[] Witnesses { get; set; }

        public int Size =>
            ResponseTxSignature.GetVarSize() +  // Oracle Response Transaction Signature
            OraclePub.Size +                    // Oracle Node Public key
            Witnesses.GetVarSize() +            // Witnesses
            UInt256.Length;                     // RequestTx Hash

        public UInt256 Hash => new UInt256(Crypto.Hash256(this.GetHashData()));

        public InventoryType InventoryType => InventoryType.Oracle;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witnesses = reader.ReadSerializableArray<Witness>(1);
            if (Witnesses.Length != 1) throw new FormatException();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            OraclePub = reader.ReadSerializable<ECPoint>();
            RequestTxHash = reader.ReadSerializable<UInt256>();
            ResponseTxSignature = reader.ReadFixedBytes(64);
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Witnesses);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(OraclePub);
            writer.Write(RequestTxHash);
            writer.Write(ResponseTxSignature);
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
        {
            return new[] { Contract.CreateSignatureRedeemScript(OraclePub).ToScriptHash() };
        }

        public bool Verify(StoreView snapshot)
        {
            ECPoint[] validators = NativeContract.Oracle.GetOracleValidators(snapshot);
            if (!validators.Any(u => u.Equals(OraclePub))) return false;
            return this.VerifyWitnesses(snapshot, MaxWitnessGas);
        }
    }
}
