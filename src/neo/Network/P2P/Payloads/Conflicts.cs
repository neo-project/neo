using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Indicates that the transaction is an Notrary tx.
    /// </summary>
    public class Conflicts : TransactionAttribute
    {
        public UInt256 Hash;

        public override TransactionAttributeType Type => TransactionAttributeType.ConflictsT;

        public override bool AllowMultiple => true;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Hash = new UInt256(reader.ReadVarBytes(UInt256.Length));
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.WriteVarBytes(Hash.ToArray());
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hash"] = Hash.ToString();
            return json;
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            if (NativeContract.Ledger.ContainsTransaction(snapshot, Hash)) return false;
            return true;
        }
    }
}
