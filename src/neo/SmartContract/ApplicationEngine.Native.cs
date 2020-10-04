using Neo.Ledger;
using Neo.SmartContract.Native;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor Neo_Native_Deploy = Register("Neo.Native.Deploy", nameof(DeployNativeContracts), 0, CallFlags.AllowModifyStates, false);
        public static readonly InteropDescriptor Neo_Native_Call = Register("Neo.Native.Call", nameof(CallNativeContract), 0, CallFlags.None, false);

        protected internal void DeployNativeContracts()
        {
            if (Snapshot.PersistingBlock.Index != 0)
                throw new InvalidOperationException();
            foreach (NativeContract contract in NativeContract.Contracts)
            {
                Snapshot.Contracts.Add(contract.Hash, new ContractState
                {
                    Id = contract.Id,
                    Script = contract.Script,
                    ScriptHash = contract.Hash, // Not NefHash here
                    Abi = contract.Abi,
                    Manifest = contract.Manifest
                });
                contract.Initialize(this);
            }
        }

        protected internal void CallNativeContract(string name)
        {
            NativeContract.GetContract(name).Invoke(this);
        }
    }
}
