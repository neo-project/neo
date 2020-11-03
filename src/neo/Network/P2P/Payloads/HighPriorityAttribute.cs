using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class HighPriorityAttribute : TransactionAttribute
    {
        public override bool AllowMultiple => false;
        public override TransactionAttributeType Type => TransactionAttributeType.HighPriority;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
        }

        public override bool Verify(StoreView snapshot, Transaction tx)
        {
            UInt160 committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            return tx.Signers.Any(p => p.Account.Equals(committee));
        }
    }
}
