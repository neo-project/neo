using Neo.Cryptography;
using Neo.IO;
using Neo.VM;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class P2PNotaryRequestPayload : ISerializable
    {
        private static uint NetWork => ProtocolSettings.Default.Network;
        private Transaction MainTransaction;
        private Transaction FallbackTransaction;
        private Witness Witness;
        private UInt256 Hash;
        private UInt256 SignedHash;

        public int Size => MainTransaction.Size + FallbackTransaction.Size + Witness.Size;

        public P2PNotaryRequestPayload(byte[] value)
        {
            using MemoryStream ms = new MemoryStream(value);
            using BinaryReader reader = new(ms);
            Deserialize(reader);
        }

        public P2PNotaryRequestPayload(Transaction mainTx, Transaction fbTx, Witness witness)
        {
            MainTransaction = mainTx;
            FallbackTransaction = fbTx;
            Witness = witness;
        }

        public void Deserialize(BinaryReader reader)
        {
            DeserializeHashableFields(reader);
            Witness = reader.ReadSerializable<Witness>();
        }

        public void Serialize(BinaryWriter writer)
        {
            SerializeHashableFields(writer);
            writer.Write(Witness);
        }

        public void DeserializeHashableFields(BinaryReader reader)
        {
            MainTransaction = reader.ReadSerializable<Transaction>();
            FallbackTransaction = reader.ReadSerializable<Transaction>();
            IsValid();
            CreateHash();
        }

        public void SerializeHashableFields(BinaryWriter writer)
        {
            writer.Write(MainTransaction);
            writer.Write(FallbackTransaction);
        }

        public UInt256 GetSignedHash()
        {
            if (SignedHash is null) CreateHash();
            return SignedHash;
        }

        public byte[] GetSignedPart()
        {
            if (Hash is null) CreateHash();
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);
            writer.Write(NetWork);
            writer.Write(Hash.ToArray());
            return ms.ToArray();
        }

        public void CreateHash()
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);
            SerializeHashableFields(writer);
            Hash = new UInt256(ms.ToArray().Sha256());
            byte[] signed = GetSignedPart();
            SignedHash = new UInt256(signed.Sha256());
        }

        public void IsValid()
        {
            var nKeysMain = MainTransaction.GetAttributes<NotaryAssisted>();
            if (nKeysMain.Count() == 0) throw new Exception("main transaction should have NotaryAssisted attribute");
            if (nKeysMain.ToArray()[0].NKeys == 0) throw new Exception("main transaction should have NKeys > 0");
            if (FallbackTransaction.Signers.Length != 2) throw new Exception("fallback transaction should have two signers");
            if (FallbackTransaction.Witnesses[0].InvocationScript.Length != 66
                || FallbackTransaction.Witnesses[0].VerificationScript.Length != 0
                || (FallbackTransaction.Witnesses[0].InvocationScript[0] != (byte)OpCode.PUSHDATA1 && FallbackTransaction.Witnesses[0].InvocationScript[1] != 64))
                throw new Exception("fallback transaction has invalid dummy Notary witness");
            if (FallbackTransaction.GetAttribute<NotValidBefore>() is null) throw new Exception("fallback transactions should have NotValidBefore attribute");
            var conflicts = FallbackTransaction.GetAttributes<Conflicts>();
            if (conflicts.Count() != 1) throw new Exception("fallback transaction should have one Conflicts attribute");
            if (conflicts.ToArray()[0].Hash != MainTransaction.Hash) throw new Exception("fallback transaction does not conflicts with the main transaction");
            var nKeysFallback = FallbackTransaction.GetAttributes<NotaryAssisted>();
            if (nKeysFallback.Count() == 0) throw new Exception("fallback transaction should have NotaryAssisted attribute");
            if (nKeysFallback.ToArray()[0].NKeys != 0) throw new Exception("fallback transaction should have NKeys = 0");
            if (MainTransaction.ValidUntilBlock != FallbackTransaction.ValidUntilBlock) throw new Exception("both main and fallback transactions should have the same ValidUntil value");
        }
    }
}
