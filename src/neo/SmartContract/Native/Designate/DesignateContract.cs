#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Persistence;
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
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetDesignatedByRole(StoreView snapshot, Role role, uint index)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (snapshot.Height + 1 < index)
                throw new ArgumentOutOfRangeException(nameof(index));
            byte[] key = CreateStorageKey((byte)role).AddBigEndian(index).ToArray();
            byte[] boundary = CreateStorageKey((byte)role).ToArray();
            return snapshot.Storages.FindRange(key, boundary, SeekDirection.Backward)
                .Select(u => u.Value.GetInteroperable<NodeList>().ToArray())
                .FirstOrDefault() ?? System.Array.Empty<ECPoint>();
        }

        [ContractMethod(0_01000000, CallFlags.AllowModifyStates)]
        private byte[] GetDesignatedInfo(StoreView snapshot, Role role, ECPoint node)
        {
            var nodes = GetDesignatedByRole(snapshot, role, snapshot.HeaderHeight);
            if (!nodes.Contains(node))
                throw new InvalidOperationException("Node not found");

            var value = snapshot.Storages.TryGet(CreateStorageKey((byte)role).Add(node));
            return value?.Value ?? System.Array.Empty<byte>();
        }

        [ContractMethod(0_05000000, CallFlags.AllowModifyStates)]
        private void SetDesignatedInfo(ApplicationEngine engine, Role role, ECPoint node, byte[] value)
        {
            if (!engine.CheckWitness(node.EncodePoint(true)))
                throw new InvalidOperationException("Wrong signed by node");

            var nodes = GetDesignatedByRole(engine.Snapshot, role, engine.Snapshot.HeaderHeight);
            if (!nodes.Contains(node))
                throw new InvalidOperationException("Node not found");

            if (value == null || value.Length == 0)
            {
                engine.Snapshot.Storages.Delete(CreateStorageKey((byte)role).Add(node));
            }
            else
            {
                engine.Snapshot.Storages.GetAndChange(CreateStorageKey((byte)role).Add(node), () => new StorageItem(value, false)).Value = value;
            }
        }

        [ContractMethod(0, CallFlags.AllowModifyStates)]
        private void DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
        {
            if (nodes.Length == 0 || nodes.Length > 32)
                throw new ArgumentException();
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!CheckCommittee(engine))
                throw new InvalidOperationException(nameof(DesignateAsRole));
            if (engine.Snapshot.PersistingBlock is null)
                throw new InvalidOperationException(nameof(DesignateAsRole));
            uint index = engine.Snapshot.PersistingBlock.Index + 1;
            var key = CreateStorageKey((byte)role).AddBigEndian(index);
            if (engine.Snapshot.Storages.Contains(key))
                throw new InvalidOperationException();

            var previous = GetDesignatedByRole(engine.Snapshot, role, engine.Snapshot.HeaderHeight);

            NodeList list = new NodeList();
            list.AddRange(nodes);
            list.Sort();
            engine.Snapshot.Storages.Add(key, new StorageItem(list));

            foreach (var prev in previous)
            {
                if (!list.Contains(prev))
                {
                    engine.Snapshot.Storages.Delete(CreateStorageKey((byte)role).Add(prev));
                }
            }
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
