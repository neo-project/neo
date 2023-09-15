using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract.Native;

namespace Neo.Network.P2P.Payloads
{
    public class Conflicts : TransactionAttribute
    {
        /// <summary>
        /// Indicates the conflict transaction hash.
        /// </summary>
        public UInt256 Hash;

        public override TransactionAttributeType Type => TransactionAttributeType.Conflicts;

        public override bool AllowMultiple => true;

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

            HashSet<UInt256> hashes = new();
            var conflictingSigners = tx.Signers.Select(s => s.Account).ToArray();

            foreach (var attr in tx.GetAttributes<Conflicts>())
            {
                if (!NativeContract.Ledger.CanFitConflictingSigners(snapshot, tx.Hash, conflictingSigners))
                {
                    // Can't fit MaxAllowedConflictingSigners

                    return false;
                }

                if (!hashes.Add(attr.Hash))
                {
                    // Duplicate attribute

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
