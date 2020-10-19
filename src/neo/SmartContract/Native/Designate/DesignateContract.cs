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
                engine.Snapshot.Storages.Add(CreateStorageKey(role), new StorageItem(new NodeMap()));
            }
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetDesignatedByRole(StoreView snapshot, Role role)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            NodeMap map = snapshot.Storages[CreateStorageKey((byte)role)].GetInteroperable<NodeMap>();
            List<uint> keys = map.Keys.ToList();
            if (keys.Count == 0) return System.Array.Empty<ECPoint>();
            return map[keys.Last()].ToArray();
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetDesignatedByRoleAndIndex(StoreView snapshot, Role role, uint index)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (snapshot.Height + 2 < index)
                throw new ArgumentOutOfRangeException(nameof(index));
            NodeMap map = snapshot.Storages[CreateStorageKey((byte)role)].GetInteroperable<NodeMap>();
            List<uint> keys = map.Keys.ToList();
            if (keys.Count == 0) return System.Array.Empty<ECPoint>();

            uint height = 0;
            foreach (uint h in keys)
            {
                if (index <= h) break;
                height = h;
            }

            if (0 < height)
                return map[height].ToArray();

            return System.Array.Empty<ECPoint>();
        }

        [ContractMethod(0, CallFlags.AllowModifyStates)]
        private bool DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
        {
            if (nodes.Length == 0 || nodes.Length > 32)
                throw new ArgumentException();
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            NodeMap map = engine.Snapshot.Storages.GetAndChange(CreateStorageKey((byte)role)).GetInteroperable<NodeMap>();
            List<ECPoint> list = nodes.ToList();
            list.Sort();
            map[engine.Snapshot.Height + 1] = list;
            return true;
        }

        private class NodeMap : Dictionary<uint, List<ECPoint>>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (var element in (Map)stackItem)
                {
                    uint height = (uint)element.Key.GetInteger();
                    List<ECPoint> list = ((VM.Types.Array)element.Value).Select(p => p.GetSpan().AsSerializable<ECPoint>()).ToList();
                    Add(height, list);
                }
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Map map = new Map(referenceCounter);
                foreach (var item in this)
                {
                    VM.Types.Array arr = new VM.Types.Array(item.Value.Select(p => (StackItem)p.ToArray()));
                    map[item.Key] = arr;
                }
                return map;
            }
        }
    }
}
