using Neo.Ledger;

namespace Neo.SmartContract.Native
{
    public sealed class ManagementContract : NativeContract
    {
        public override string Name => "Neo Contract Management";
        public override int Id => 0;
        public override uint ActiveBlockIndex => 0;

        internal override void OnPersist(ApplicationEngine engine)
        {
            foreach (NativeContract contract in Contracts)
            {
                if (contract.ActiveBlockIndex != engine.Snapshot.PersistingBlock.Index)
                    continue;
                engine.Snapshot.Contracts.Add(contract.Hash, new ContractState
                {
                    Id = contract.Id,
                    Script = contract.Script,
                    Hash = contract.Hash,
                    Manifest = contract.Manifest
                });
                contract.Initialize(engine);
            }
        }
    }
}
