using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class NotaryRequest : IInventory
    {
        /// <summary>
        /// The transaction need Notary to collect signatures.
        /// </summary>
        private Transaction mainTransaction;

        /// <summary>
        /// This transaction is valid when MainTransaction failed.
        /// </summary>
        private Transaction fallbackTransaction;

        /// <summary>
        /// The witness of the payload. It is a witness of the fallback transaction's signer.
        /// </summary>
        private Witness witness;
        private UInt256 hash = null;

        public InventoryType InventoryType => InventoryType.Notary;

        public UInt256 Hash
        {
            get
            {
                hash ??= this.CalculateHash();
                return hash;
            }
        }

        public Witness[] Witnesses
        {
            get
            {
                return new Witness[] { witness };
            }
            set
            {
                witness = value[0];
            }
        }

        public Transaction MainTransaction
        {
            get => mainTransaction;
            set
            {
                mainTransaction = value;
                hash = null;
            }
        }

        public Transaction FallbackTransaction
        {
            get => fallbackTransaction;
            set
            {
                fallbackTransaction = value;
                hash = null;
            }
        }

        public int Size => mainTransaction.Size + fallbackTransaction.Size + witness.Size;


        public void DeserializeUnsigned(BinaryReader reader)
        {
            mainTransaction = reader.ReadSerializable<Transaction>();
            fallbackTransaction = reader.ReadSerializable<Transaction>();
        }

        public void Deserialize(BinaryReader reader)
        {
            DeserializeUnsigned(reader);
            witness = reader.ReadSerializable<Witness>();
        }

        public void Serialize(BinaryWriter writer)
        {
            SerializeUnsigned(writer);
            writer.Write(witness);
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(mainTransaction);
            writer.Write(fallbackTransaction);
        }

        public UInt160[] GetScriptHashesForVerifying(DataCache snapshot)
        {
            return new UInt160[] { fallbackTransaction.Signers[1].Account };
        }

        public bool Verify(ProtocolSettings settings, DataCache snapshot)
        {
            var nKeysMain = mainTransaction.GetAttributes<NotaryAssisted>();
            if (!nKeysMain.Any()) return false;
            if (nKeysMain.ToArray()[0].NKeys == 0) return false;
            if (fallbackTransaction.Signers.Length != 2) return false;
            if (fallbackTransaction.Witnesses[0].InvocationScript.Length != 66
                || fallbackTransaction.Witnesses[0].VerificationScript.Length != 0
                || fallbackTransaction.Witnesses[0].InvocationScript[0] != (byte)OpCode.PUSHDATA1 || fallbackTransaction.Witnesses[0].InvocationScript[1] != 64)
                return false;
            if (fallbackTransaction.Sender != NativeContract.Notary.Hash) return false;
            var nvb = fallbackTransaction.GetAttribute<NotValidBefore>();
            if (nvb is null) return false;
            var maxNVBDelta = NativeContract.Notary.GetMaxNotValidBeforeDelta(snapshot);
            var block_height = NativeContract.Ledger.CurrentIndex(snapshot);
            if (block_height + maxNVBDelta < nvb.Height) return false;
            if (nvb.Height + maxNVBDelta < fallbackTransaction.ValidUntilBlock) return false;
            var conflicts = fallbackTransaction.GetAttributes<ConflictAttribute>();
            if (conflicts.Count() != 1) return false;
            if (conflicts.ToArray()[0].Hash != mainTransaction.Hash) return false;
            var nKeysFallback = fallbackTransaction.GetAttributes<NotaryAssisted>();
            if (!nKeysFallback.Any()) return false;
            if (nKeysFallback.ToArray()[0].NKeys != 0) return false;
            if (mainTransaction.ValidUntilBlock != fallbackTransaction.ValidUntilBlock) return false;
            if (!fallbackTransaction.VerifyWitness(settings, null, fallbackTransaction.Signers[1].Account, fallbackTransaction.Witnesses[1], SmartContract.Helper.MaxVerificationGas, out _)) return false;
            return this.VerifyWitnesses(settings, null, SmartContract.Helper.MaxVerificationGas);
        }
    }
}
