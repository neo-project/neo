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
                engine.Snapshot.Storages.Add(CreateStorageKey(role), new StorageItem(BitConverter.GetBytes(0u)));
                engine.Snapshot.Storages.Add(CreateStorageKey(role).Add(0u), new StorageItem(new NodeList()));
            }
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetDesignatedByRole(StoreView snapshot, Role role)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            byte[] index = snapshot.Storages[CreateStorageKey((byte)role)].Value;
            KeyBuilder index_key = CreateStorageKey((byte)role).Add(index);
            return snapshot.Storages[index_key].GetInteroperable<NodeList>().ToArray();
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetDesignatedByRoleAndIndex(StoreView snapshot, Role role, uint index)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (snapshot.Height + 2 < index)
                throw new ArgumentOutOfRangeException(nameof(index));
            List<uint> keys = snapshot.Storages.Find(CreateStorageKey((byte)role).ToArray())
                .Select(p => p.Key.Key.Skip(1).ToArray())
                .Where(p => p.Length == sizeof(uint))
                .Select(p => BitConverter.ToUInt32(p))
                .ToList();
            keys.Sort();

            if (keys.Count == 0) return System.Array.Empty<ECPoint>();

            uint height = 0;
            foreach (uint h in keys)
            {
                if (index <= h) break;
                height = h;
            }

            if (0 < height)
                return snapshot.Storages[CreateStorageKey((byte)role).Add(height)].GetInteroperable<NodeList>().ToArray();

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
            uint index = engine.Snapshot.Height + 1;
            NodeList list = engine.Snapshot.Storages.GetAndChange(CreateStorageKey((byte)role).Add(index), () => new StorageItem(new NodeList())).GetInteroperable<NodeList>();
            list.Clear();
            list.AddRange(nodes);
            list.Sort();
            StorageItem current = engine.Snapshot.Storages.GetAndChange(CreateStorageKey((byte)role));
            current.Value = BitConverter.GetBytes(index);
            return true;
        }

        private class NodeList : List<ECPoint>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (VM.Types.Array)stackItem)
                    Add(item.GetSpan().AsSerializable<ECPoint>());
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new VM.Types.Array(referenceCounter, this.Select(p => (StackItem)p.ToArray()));
            }
        }
    }
}
