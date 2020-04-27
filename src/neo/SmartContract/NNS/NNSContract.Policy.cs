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
        protected const byte Prefix_ReceiptAddress = 28;

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

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_ReceiptAddress), new StorageItem
            {
                Value = new UInt160().ToArray() //TBD
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
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Array }, ParameterNames = new[] { "pubkeys" })]
        private StackItem SetAdmin(ApplicationEngine engine, Array args)
        {
            //verify multi-signature of committees
            ECPoint[] committees = NEO.GetCommittee(engine.Snapshot);
            UInt160 script = Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 3, committees).ToScriptHash();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, script))
                return false;

            ECPoint[] pubkeys = args[0].GetSpan().AsSerializableArray<ECPoint>();
            StorageKey key = CreateStorageKey(Prefix_Admin);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = pubkeys.ToByteArray();
            return true;
        }

        // set rental price, only can be called by admin
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "value" })]
        private StackItem SetRentalPrice(ApplicationEngine engine, Array args)
        {
            ECPoint[] admins = GetAdmin(engine.Snapshot);
            UInt160 script = Contract.CreateMultiSigRedeemScript(admins.Length - (admins.Length - 1) / 3, admins).ToScriptHash();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, script)) return false;

            uint value = (uint)args[0].GetBigInteger();
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RentalPrice));
            storage.Value = BitConverter.GetBytes(value);
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "address" })]
        private StackItem SetReceiptAddress(ApplicationEngine engine, Array args)
        {
            ECPoint[] admins = GetAdmin(engine.Snapshot);
            UInt160 script = Contract.CreateMultiSigRedeemScript(admins.Length - (admins.Length - 1) / 3, admins).ToScriptHash();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, script)) return false;

            UInt160 address = new UInt160(args[0].GetSpan());
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_ReceiptAddress));
            storage.Value = address.ToArray();
            return true;
        }
    }
}
