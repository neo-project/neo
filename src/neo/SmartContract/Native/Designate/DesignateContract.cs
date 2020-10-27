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
using System.Buffers.Binary;
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
                engine.Snapshot.Storages.Add(CreateStorageKey(role), new StorageItem(BitConverter.GetBytes(0u))); // Same as BigEndian
                engine.Snapshot.Storages.Add(CreateStorageKey(role).AddBigEndian(0u), new StorageItem(new NodeList()));
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
                .Where(p => p.Key.Key.Length == sizeof(uint) + 1)
                .Select(p => BinaryPrimitives.ReadUInt32BigEndian(p.Key.Key.AsSpan(1, sizeof(uint))))
                .ToList();

            if (keys.Count == 0) return System.Array.Empty<ECPoint>();

            keys.Sort();
            uint height = 0;
            foreach (uint h in keys)
            {
                if (index <= h) break;
                height = h;
            }

            if (0 < height)
                return snapshot.Storages[CreateStorageKey((byte)role).AddBigEndian(height)].GetInteroperable<NodeList>().ToArray();

            return System.Array.Empty<ECPoint>();
        }

        [ContractMethod(0, CallFlags.AllowModifyStates)]
        private void DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
        {
            if (nodes.Length == 0 || nodes.Length > 32)
                throw new ArgumentException();
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            uint index = engine.Snapshot.Height + 1;
            var key = CreateStorageKey((byte)role).AddBigEndian(index);
            if (engine.Snapshot.Storages.Contains(key))
                throw new InvalidOperationException();
            NodeList list = new NodeList();
            list.AddRange(nodes);
            list.Sort();
            engine.Snapshot.Storages.Add(key, new StorageItem(list));
            StorageItem current = engine.Snapshot.Storages.GetAndChange(CreateStorageKey((byte)role));
            current.Value = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(current.Value, index);
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
