using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Nns
{
    partial class NnsContract
    {        
        private const byte Prefix_Admin = 26;

        // Get Admin
        [ContractMethod(0_01000000, ContractParameterType.Hash160, CallFlags.AllowStates)]
        private StackItem GetAdmin(ApplicationEngine engine, Array args)
        {
            return GetAdmin(engine.Snapshot).ToArray();
        }

        public UInt160 GetAdmin(StoreView snapshot)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_Admin))?.Value.AsSerializable<UInt160>();
        }

        // set addmin, only can be called by committees
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "admin" })]
        private StackItem SetAdmin(ApplicationEngine engine, Array args)
        {
            //verify multi-signature of committees
            var address = NEO.GetCommitteeMultiSigAddress(engine.Snapshot);
            if (!InteropService.Runtime.CheckWitnessInternal(engine, address))
                return false;

            UInt160 admin = args[0].GetSpan().AsSerializable<UInt160>();
            StorageKey key = CreateStorageKey(Prefix_Admin);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = admin.ToArray();
            return true;
        }
    }
}
