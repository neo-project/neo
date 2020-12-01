using Neo.Ledger;
using Neo.SmartContract.Native;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor Neo_Native_Deploy = Register("Neo.Native.Deploy", nameof(DeployNativeContract), 0, CallFlags.AllowModifyStates, false);
        public static readonly InteropDescriptor Neo_Native_Call = Register("Neo.Native.Call", nameof(CallNativeContract), 0, CallFlags.None, false);

        protected internal void DeployNativeContract(UInt160 hash)
        {
            if (Trigger != TriggerType.OnPersist) throw new InvalidOperationException("Invalid trigger");
            NativeContract contract = NativeContract.GetContract(hash);
            if (contract == null) throw new Exception($"Can't find a native contract with the hash: {hash}");
            if (Snapshot.Contracts.Contains(hash)) throw new Exception($"{hash} already deployed");

            Snapshot.Contracts.Add(contract.Hash, new ContractState
            {
                Id = contract.Id,
                Script = contract.Script,
                Hash = contract.Hash, // Use the native hash
                Manifest = contract.Manifest
            });
            contract.Initialize(this);
        }

        protected internal void CallNativeContract(string name)
        {
            NativeContract.GetContract(name).Invoke(this);
        }
    }
}
