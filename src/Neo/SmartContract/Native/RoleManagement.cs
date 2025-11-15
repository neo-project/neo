// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.Extensions.IO;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native;

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
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), $"Role {role} is not valid");

        var currentIndex = Ledger.CurrentIndex(snapshot);
        if (currentIndex + 1 < index)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} exceeds current index + 1 ({currentIndex + 1})");
        var key = CreateStorageKey((byte)role, index).ToArray();
        var boundary = CreateStorageKey((byte)role).ToArray();
        return snapshot.FindRange(key, boundary, SeekDirection.Backward)
            .Select(u => u.Value.GetInteroperable<NodeList>().ToArray())
            .FirstOrDefault() ?? [];
    }

    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private void DesignateAsRole(ApplicationEngine engine, Role role, ECPoint[] nodes)
    {
        if (nodes.Length == 0 || nodes.Length > 32)
            throw new ArgumentException($"Nodes count {nodes.Length} must be between 1 and 32", nameof(nodes));
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), $"Role {role} is not valid");
        AssertCommittee(engine);

        if (engine.PersistingBlock is null)
            throw new InvalidOperationException("Persisting block is null");
        var index = engine.PersistingBlock.Index + 1;
        var key = CreateStorageKey((byte)role, index);
        if (engine.SnapshotCache.Contains(key))
            throw new InvalidOperationException("Role already designated");

        NodeList list = new();
        list.AddRange(nodes);
        list.Sort();
        engine.SnapshotCache.Add(key, new StorageItem(list));
        if (engine.IsHardforkEnabled(Hardfork.HF_Echidna))
        {
            var oldNodes = new VM.Types.Array(engine.ReferenceCounter, GetDesignatedByRole(engine.SnapshotCache, role, index - 1).Select(u => (ByteString)u.EncodePoint(true)));
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

        protected override StackItem ElementToStackItem(ECPoint element, IReferenceCounter? referenceCounter)
        {
            return element.ToArray();
        }
    }
}
