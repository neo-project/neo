using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Indicates that the transaction is an Notrary tx.
    /// </summary>
    public class NotValidBefore : TransactionAttribute
    {
        public uint Height;

        public override TransactionAttributeType Type => TransactionAttributeType.NotValidBeforeT;

        public override bool AllowMultiple => false;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            var bytes = reader.ReadVarBytes(4);
            if (bytes.Length != 4) throw new FormatException(string.Format("expected 4 bytes, got {0}", bytes.Length));
            Height = BitConverter.ToUInt32(bytes);
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.WriteVarBytes(BitConverter.GetBytes(Height));
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["height"] = Height;
            return json;
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            var nvb = tx.GetAttribute<NotValidBefore>().Height;
            var maxNVBDelta = NativeContract.Notary.GetMaxNotValidBeforeDelta(snapshot);
            var height = NativeContract.Ledger.CurrentIndex(snapshot);
            if (height < nvb) return false;
            if ((height + maxNVBDelta) < nvb) return false;
            if ((nvb + maxNVBDelta) < tx.ValidUntilBlock) return false;
            return true;
        }
    }
}
