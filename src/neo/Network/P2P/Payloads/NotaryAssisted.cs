using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Indicates that the transaction is an Notrary tx.
    /// </summary>
    public class NotaryAssisted : TransactionAttribute
    {
        public uint NKeys;

        public override TransactionAttributeType Type => TransactionAttributeType.NotaryAssistedT;

        public override bool AllowMultiple => false;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            var bytes = reader.ReadVarBytes(1);
            if (bytes.Length != 1) throw new FormatException(string.Format("expected 1 bytes, got {0}", bytes.Length));
            NKeys = BitConverter.ToUInt32(bytes);
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.WriteVarBytes(BitConverter.GetBytes(NKeys));
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["height"] = NKeys;
            return json;
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            if (tx.Signers.First(p => p.Account.Equals(NativeContract.Notary.Hash)) is null) return false;
            return true;
        }
    }
}
