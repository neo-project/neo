using Neo.IO;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Conflicts : TransactionAttribute
    {
        /// <summary>
        /// Indicates the conflict transaction hash.
        /// </summary>
        public UInt256 Hash;

        public override TransactionAttributeType Type => TransactionAttributeType.Conflicts;

        public override int AllowedCount => 4;

        public override int Size => base.Size + Hash.Size;

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
            // Check already stored signers

            var conflictingSigners = tx.Signers.Select(s => s.Account);

            foreach (var attr in tx.GetAttributes<Conflicts>())
            {
                var conflictRecord = snapshot.TryGet(
                    new KeyBuilder(NativeContract.Ledger.Id, LedgerContract.Prefix_Transaction).Add(attr.Hash))?
                    .GetInteroperable<TransactionState>();
                if (conflictRecord == null) continue;

                if (conflictRecord.ConflictingSigners.Concat(conflictingSigners).Distinct().Count() > 100)
                {
                    return false;
                }
            }

            // Only check if conflicting transaction is on chain. It's OK if the
            // conflicting transaction was in the Conflicts attribute of some other
            // on-chain transaction.
            return !NativeContract.Ledger.ContainsTransaction(snapshot, Hash);
        }
    }
}
