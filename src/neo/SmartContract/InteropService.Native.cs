using Neo.Ledger;
using Neo.SmartContract.Native;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        internal static class Native
        {
            public static readonly InteropDescriptor Deploy = Register("Neo.Native.Deploy", Native_Deploy, 0, TriggerType.Application, CallFlags.AllowModifyStates);

            static Native()
            {
                foreach (NativeContract contract in NativeContract.Contracts)
                    Register(contract.ServiceName, contract.Invoke, contract.GetPrice, TriggerType.System | TriggerType.Application, CallFlags.None);
            }

            private static bool Native_Deploy(ApplicationEngine engine)
            {
                if (engine.Snapshot.PersistingBlock.Index != 0) return false;
                foreach (NativeContract contract in NativeContract.Contracts)
                {
                    engine.Snapshot.Contracts.Add(contract.Hash, new ContractState
                    {
                        Script = contract.Script,
                        Manifest = contract.Manifest
                    });
                    contract.Initialize(engine);
                }
                return true;
            }
        }
    }
}
