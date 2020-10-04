#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Native.Designate
{
    public sealed class DesignateContract : NativeContract
    {
        public override string Name => "Designation";
        public override int Id => -5;

        internal DesignateContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            foreach (byte role in Enum.GetValues(typeof(Role)))
            {
                engine.Snapshot.Storages.Add(CreateStorageKey(role), new StorageItem(new NodeList()));
            }
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetDesignatedByRole(StoreView snapshot, Role role)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            return snapshot.Storages[CreateStorageKey((byte)role)].GetInteroperable<NodeList>().ToArray();
        }

        [ContractMethod(0, CallFlags.AllowModifyStates)]
        private void DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
        {
            if (nodes.Length == 0 || nodes.Length > StorageItem.MaxInteropEntries) throw new ArgumentException(nameof(nodes));
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            NodeList list = engine.Snapshot.Storages.GetAndChange(CreateStorageKey((byte)role)).GetInteroperable<NodeList>();
            list.Clear();
            list.AddRange(nodes);
            list.Sort();
        }

        private class NodeList : List<ECPoint>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (VM.Types.Array)stackItem)
                    Add(ECPoint.DecodePoint(item.GetSpan(), ECCurve.Secp256r1));
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new VM.Types.Array(referenceCounter, this.Select(p => (StackItem)p.ToArray()));
            }
        }
    }
}
