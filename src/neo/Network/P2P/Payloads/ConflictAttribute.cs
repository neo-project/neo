using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ConflictAttribute : TransactionAttribute
    {
        /// <summary>
        /// Indicates the conflict transaction hash.
        /// </summary>
        public UInt256 Hash;

        public override TransactionAttributeType Type => TransactionAttributeType.Conflict;

        public override bool AllowMultiple => true;

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
            Hash = reader.ReadSerializable<UInt256>();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Hash);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hash"] = Hash.ToString();
            return json;
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            return !NativeContract.Ledger.ContainsTransaction(snapshot, Hash);
        }
    }
}
