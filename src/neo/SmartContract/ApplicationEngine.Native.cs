using Neo.SmartContract.Native;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor Neo_Native_Call = Register("Neo.Native.Call", nameof(CallNativeContract), 0, CallFlags.None, false);
        public static readonly InteropDescriptor Neo_Native_OnPersist = Register("Neo.Native.OnPersist", nameof(NativeOnPersist), 0, CallFlags.None, false);
        public static readonly InteropDescriptor Neo_Native_PostPersist = Register("Neo.Native.PostPersist", nameof(NativePostPersist), 0, CallFlags.None, false);

        protected internal void CallNativeContract(string name)
        {
            NativeContract contract = NativeContract.GetContract(name);
            if (contract is null || contract.ActiveBlockIndex > Snapshot.PersistingBlock.Index)
                throw new InvalidOperationException();
            contract.Invoke(this);
        }

        protected internal void NativeOnPersist()
        {
            if (Trigger != TriggerType.OnPersist)
                throw new InvalidOperationException();
            foreach (NativeContract contract in NativeContract.Contracts)
                if (contract.ActiveBlockIndex <= Snapshot.PersistingBlock.Index)
                    contract.OnPersist(this);
        }

        protected internal void NativePostPersist()
        {
            if (Trigger != TriggerType.PostPersist)
                throw new InvalidOperationException();
            foreach (NativeContract contract in NativeContract.Contracts)
                if (contract.ActiveBlockIndex <= Snapshot.PersistingBlock.Index)
                    contract.PostPersist(this);
        }
    }
}
