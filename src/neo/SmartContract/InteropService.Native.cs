using Neo.Ledger;
using Neo.SmartContract.Native;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        internal static class Native
        {
            public static readonly InteropDescriptor Deploy = Register("Neo.Native.Deploy", Native_Deploy, 0, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Call = Register("Neo.Native.Call", Native_Call, 0, TriggerType.System | TriggerType.Application, CallFlags.None);

            private static bool Native_Deploy(ApplicationEngine engine)
            {
                if (engine.Snapshot.PersistingBlock.Index != 0) return false;
                foreach (NativeContract contract in NativeContract.Contracts)
                {
                    engine.Snapshot.Contracts.Add(contract.Hash, new ContractState
                    {
                        Id = contract.Id,
                        Script = contract.Script,
                        Manifest = contract.Manifest
                    });
                    contract.Initialize(engine);
                }
                return true;
            }

            private static bool Native_Call(ApplicationEngine engine)
            {
                if (!engine.TryPop(out string name)) return false;
                NativeContract contract = NativeContract.GetContract(name);
                if (contract is null) return false;
                contract.Invoke(engine);
                return true;
            }
        }
    }
}
