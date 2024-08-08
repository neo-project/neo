// Copyright (C) 2015-2024 The Neo Project.
//
// RoleManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract for managing roles in NEO system.
    /// </summary>
    public sealed class RoleManagement : NativeContract
    {
        [ContractEvent(0, name: "Designation",
            "Role", ContractParameterType.Integer,
            "BlockIndex", ContractParameterType.Integer,
            Hardfork.HF_Echidna)]

        [ContractEvent(Hardfork.HF_Echidna, 0, name: "Designation",
            "Role", ContractParameterType.Integer,
            "BlockIndex", ContractParameterType.Integer,
            "Old", ContractParameterType.Array,
            "New", ContractParameterType.Array
            )]

        internal RoleManagement() : base() { }

        /// <summary>
        /// Gets the list of nodes for the specified role.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="role">The type of the role.</param>
        /// <param name="index">The index of the block to be queried.</param>
        /// <returns>The public keys of the nodes.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public ECPoint[] GetDesignatedByRole(DataCache snapshot, Role role, uint index)
        {
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (Ledger.CurrentIndex(snapshot) + 1 < index)
                throw new ArgumentOutOfRangeException(nameof(index));
            byte[] key = CreateStorageKey((byte)role).AddBigEndian(index).ToArray();
            byte[] boundary = CreateStorageKey((byte)role).ToArray();
            return snapshot.FindRange(key, boundary, SeekDirection.Backward)
                .Select(u => u.Value.GetInteroperable<NodeList>().ToArray())
                .FirstOrDefault() ?? System.Array.Empty<ECPoint>();
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private void DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
        {
            if (nodes.Length == 0 || nodes.Length > 32)
                throw new ArgumentException(null, nameof(nodes));
            if (!Enum.IsDefined(typeof(Role), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!CheckCommittee(engine))
                throw new InvalidOperationException(nameof(DesignateAsRole));
            if (engine.PersistingBlock is null)
                throw new InvalidOperationException(nameof(DesignateAsRole));
            uint index = engine.PersistingBlock.Index + 1;
            var key = CreateStorageKey((byte)role).AddBigEndian(index);
            if (engine.SnapshotCache.Contains(key))
                throw new InvalidOperationException();
            NodeList list = new();
            list.AddRange(nodes);
            list.Sort();
            engine.SnapshotCache.Add(key, new StorageItem(list));
            
            if (engine.IsHardforkEnabled(Hardfork.HF_Echidna))
            {
                var oldNodes = new VM.Types.Array(engine.ReferenceCounter, GetDesignatedByRole(engine.Snapshot, role, index - 1).Select(u => (ByteString)u.EncodePoint(true)));
                var newNodes = new VM.Types.Array(engine.ReferenceCounter, nodes.Select(u => (ByteString)u.EncodePoint(true)));

                engine.SendNotification(Hash, "Designation", new VM.Types.Array(engine.ReferenceCounter, [(int)role, engine.PersistingBlock.Index, oldNodes, newNodes]));
            }
            else
            {
                engine.SendNotification(Hash, "Designation", new VM.Types.Array(engine.ReferenceCounter, [(int)role, engine.PersistingBlock.Index]));
            }
        }

        private class NodeList : InteroperableList<ECPoint>
        {
            protected override ECPoint ElementFromStackItem(StackItem item)
            {
                return ECPoint.DecodePoint(item.GetSpan(), ECCurve.Secp256r1);
            }

            protected override StackItem ElementToStackItem(ECPoint element, ReferenceCounter referenceCounter)
            {
                return element.ToArray();
            }
        }
    }
}
