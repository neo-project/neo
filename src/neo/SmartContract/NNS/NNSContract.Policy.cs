using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NnsContract
    {
        protected const byte Prefix_Root = 24;
        protected const byte Prefix_Record = 25;
        protected const byte Prefix_Admin = 26;
        protected const byte Prefix_RentalPrice = 27;
        protected const byte Prefix_ReceiptAddress = 28;

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

        // Get receipt address
        [ContractMethod(0_01000000, ContractParameterType.Hash160, CallFlags.AllowStates)]
        private StackItem GetReceiptAddress(ApplicationEngine engine, Array args)
        {
            return GetReceiptAddress(engine.Snapshot).ToArray();
        }

        public UInt160 GetReceiptAddress(StoreView snapshot)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_ReceiptAddress))?.Value.AsSerializable<UInt160>();
        }

        // Get rental price
        [ContractMethod(0_01000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        private StackItem GetRentalPrice(ApplicationEngine engine, Array args)
        {
            return GetRentalPrice(engine.Snapshot);
        }

        public BigInteger GetRentalPrice(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_RentalPrice));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
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

        // set rental price, only can be called by admin， 200 million blocks per year
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetRentalPrice(ApplicationEngine engine, Array args)
        {
            if (!IsAdminCalling(engine)) return false;

            BigInteger value = (long)args[0].GetBigInteger();
            if (value < 0) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RentalPrice));
            storage.Value = value.ToByteArray();
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "address" })]
        private StackItem SetReceiptAddress(ApplicationEngine engine, Array args)
        {
            if (!IsAdminCalling(engine)) return false;

            UInt160 address = new UInt160(args[0].GetSpan());
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_ReceiptAddress));
            storage.Value = address.ToArray();
            return true;
        }
    }
}
