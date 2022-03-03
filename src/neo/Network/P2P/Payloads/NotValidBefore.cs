using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class NotValidBefore : TransactionAttribute
    {
        /// <summary>
        /// Indicates that the transaction is not valid before this height.
        /// </summary>
        public uint Height;

        public override TransactionAttributeType Type => TransactionAttributeType.NotValidBefore;

        public override bool AllowMultiple => false;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Height = reader.ReadUInt32();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Height);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["height"] = Height;
            return json;
        }

        public override bool Verify(DataCache snapshot, Transaction tx)
        {
            var block_height = NativeContract.Ledger.CurrentIndex(snapshot);
            return block_height >= Height;
        }
    }
}
