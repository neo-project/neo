using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    public partial class NNSContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.NNS";
        public override int Id => -5;
        public string Name => "NNS";
        public string Symbol => "nns";
        public byte Decimals => 0;
        public const string DomainRegex = @"^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62}){1,3}$";
        public const string RootRegex = @"^[a-zA-Z]{0,62}$";

        protected const byte Prefix_Root = 22;
        protected const byte Prefix_Domain = 23;
        protected const byte Prefix_Record = 24;
        protected const byte Prefix_OwnershipMapping = 25;
        protected const byte Prefix_Admin = 26;
        protected const byte Prefix_RentalPrice = 27;

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Root), new StorageItem
            {
                Value = new UInt256[0].ToByteArray()
            });

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Admin), new StorageItem
            {
                Value = new UInt160[0].ToByteArray()
            });
            return true;
        }

        // Get Admin List
        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates)]
        private StackItem GetAdmin(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetAdmin(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public ECPoint[] GetAdmin(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_Admin)].Value.AsSerializableArray<ECPoint>();
        }

        // set addmin, only can be called by committees
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.PublicKey }, ParameterNames = new[] { "address" })]
        private StackItem SetAdmin(ApplicationEngine engine, Array args)
        {
            //verify multi-signature of committees
            ECPoint[] committees = NEO.GetCommittee(engine.Snapshot);
            UInt160 script = Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 3, committees).ToScriptHash();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, script))
                return false;

            ECPoint pubkey = args[0].GetSpan().AsSerializable<ECPoint>();
            StorageKey key = CreateStorageKey(Prefix_Admin);
            StorageItem storage = engine.Snapshot.Storages[key];
            SortedSet<ECPoint> admins = new SortedSet<ECPoint>(storage.Value.AsSerializableArray<ECPoint>());
            if (!admins.Add(pubkey)) return false;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = admins.ToByteArray();
            return true;
        }

        // set rental price
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetRentalPrice(ApplicationEngine engine, Array args)
        {
            uint value = (uint)args[0].GetBigInteger();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RentalPrice));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }
    }
}
