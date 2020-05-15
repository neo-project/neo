using Neo.Ledger;
using Neo.SmartContract.Native;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        [InteropService("Neo.Native.Deploy", 0, TriggerType.Application, CallFlags.AllowModifyStates)]
        private bool Native_Deploy()
        {
            if (Snapshot.PersistingBlock.Index != 0) return false;
            foreach (NativeContract contract in NativeContract.Contracts)
            {
                Snapshot.Contracts.Add(contract.Hash, new ContractState
                {
                    Id = contract.Id,
                    Script = contract.Script,
                    Manifest = contract.Manifest
                });
                contract.Initialize(this);
            }
            return true;
        }
    }
}
