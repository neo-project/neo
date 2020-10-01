#pragma warning disable IDE0051

using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Native.Designate
{
    public sealed class DesignateContract : NativeContract
    {
        public override string Name => "Designation";
        public override int Id => -5;

        private const int MaxRoleEntries = 32;

        internal DesignateContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            foreach (byte role in Enum.GetValues(typeof(Role)))
            {
                engine.Snapshot.Storages.Add(CreateStorageKey(role), new StorageItem(new byte[1]));
            }
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetDesignatedByRole(StoreView snapshot, Role role)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            return snapshot.Storages[CreateStorageKey((byte)role)].GetSerializableList<ECPoint>(MaxRoleEntries).ToArray();
        }

        [ContractMethod(0, CallFlags.AllowModifyStates)]
        private void DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
        {
            if (nodes.Length == 0 || nodes.Length > MaxRoleEntries) throw new ArgumentException(nameof(nodes));
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            List<ECPoint> list = engine.Snapshot.Storages.GetAndChange(CreateStorageKey((byte)role)).GetSerializableList<ECPoint>(MaxRoleEntries);
            list.Clear();
            list.AddRange(nodes);
            list.Sort();
        }
    }
}
