// Copyright (C) 2015-2025 The Neo Project.
//
// ContractManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.Extensions;
using Neo.Extensions.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System.Buffers.Binary;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native;

/// <summary>
/// A native contract used to manage all deployed smart contracts.
/// </summary>
public sealed class ContractManagement : NativeContract
{
    private const byte Prefix_MinimumDeploymentFee = 20;
    private const byte Prefix_NextAvailableId = 15;
    private const byte Prefix_Contract = 8;
    private const byte Prefix_ContractHash = 12;

    [ContractEvent(0, name: "Deploy", "Hash", ContractParameterType.Hash160)]
    [ContractEvent(1, name: "Update", "Hash", ContractParameterType.Hash160)]
    [ContractEvent(2, name: "Destroy", "Hash", ContractParameterType.Hash160)]
    internal ContractManagement() : base() { }

    private int GetNextAvailableId(DataCache snapshot)
    {
        StorageItem item = snapshot.GetAndChange(CreateStorageKey(Prefix_NextAvailableId))!;
        int value = (int)(BigInteger)item;
        item.Add(1);
        return value;
    }

    internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
    {
        if (hardfork == ActiveIn)
        {
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_MinimumDeploymentFee), new StorageItem(10_00000000));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_NextAvailableId), new StorageItem(1));
        }
        return ContractTask.CompletedTask;
    }

    private async ContractTask OnDeployAsync(ApplicationEngine engine, ContractState contract, StackItem data, bool update)
    {
        ContractMethodDescriptor? md = contract.Manifest.Abi.GetMethod(ContractBasicMethod.Deploy, ContractBasicMethod.DeployPCount);
        if (md is not null)
            await engine.CallFromNativeContractAsync(Hash, contract.Hash, md.Name, data, update);
        engine.SendNotification(Hash, update ? "Update" : "Deploy", new Array(engine.ReferenceCounter) { contract.Hash.ToArray() });
    }

    internal override async ContractTask OnPersistAsync(ApplicationEngine engine)
    {
        foreach (NativeContract contract in Contracts)
        {
            if (contract.IsInitializeBlock(engine.ProtocolSettings, engine.PersistingBlock!.Index, out var hfs))
            {
                ContractState contractState = contract.GetContractState(engine.ProtocolSettings, engine.PersistingBlock.Index);
                StorageItem? state = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Contract, contract.Hash));

                if (state is null)
                {
                    // Create the contract state
                    engine.SnapshotCache.Add(CreateStorageKey(Prefix_Contract, contract.Hash), new StorageItem(contractState));
                    engine.SnapshotCache.Add(CreateStorageKey(Prefix_ContractHash, contract.Id), new StorageItem(contract.Hash.ToArray()));

                    // Initialize the native smart contract if it's active starting from the genesis.
                    // If it's not the case, then hardfork-based initialization will be performed down below.
                    if (contract.ActiveIn is null)
                    {
                        await contract.InitializeAsync(engine, null);
                    }
                }
                else
                {
                    // Parse old contract
                    using var sealInterop = state.GetInteroperable(out ContractState oldContract, false);
                    // Increase the update counter
                    oldContract.UpdateCounter++;
                    // Modify nef and manifest
                    oldContract.Nef = contractState.Nef;
                    oldContract.Manifest = contractState.Manifest;
                }

                // Initialize native contract for all hardforks that are active starting from the persisting block.
                // If the contract is active starting from some non-nil hardfork, then this hardfork is also included into hfs.
                if (hfs?.Length > 0)
                {
                    foreach (var hf in hfs)
                    {
                        await contract.InitializeAsync(engine, hf);
                    }
                }

                // Emit native contract notification
                engine.SendNotification(Hash, state is null ? "Deploy" : "Update", new Array(engine.ReferenceCounter) { contract.Hash.ToArray() });
            }
        }
    }

    /// <summary>
    /// Gets the minimum deployment fee for deploying a contract.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>The minimum deployment fee for deploying a contract.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
#pragma warning disable CA1859
    private long GetMinimumDeploymentFee(IReadOnlyStore snapshot)
#pragma warning restore CA1859
    {
        // In the unit of datoshi, 1 datoshi = 1e-8 GAS
        return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_MinimumDeploymentFee)];
    }

    /// <summary>
    /// Sets the minimum deployment fee for deploying a contract. Only committee members can call this method.
    /// </summary>
    /// <param name="engine">The engine used to write data.</param>
    /// <param name="value">The minimum deployment fee for deploying a contract.</param>
    /// <exception cref="InvalidOperationException">Thrown when the caller is not a committee member.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
    private void SetMinimumDeploymentFee(ApplicationEngine engine, BigInteger value/* In the unit of datoshi, 1 datoshi = 1e-8 GAS*/)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "cannot be negative");
        AssertCommittee(engine);
        engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_MinimumDeploymentFee))!.Set(value);
    }

    /// <summary>
    /// Gets the deployed contract with the specified hash.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="hash">The hash of the deployed contract.</param>
    /// <returns>The deployed contract.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public ContractState? GetContract(IReadOnlyStore snapshot, UInt160 hash)
    {
        var key = CreateStorageKey(Prefix_Contract, hash);
        return snapshot.TryGet(key, out var item) ? item.GetInteroperable<ContractState>(false) : null;
    }

    /// <summary>
    /// Check if exists the deployed contract with the specified hash.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="hash">The hash of the deployed contract.</param>
    /// <returns>True if deployed contract exists.</returns>
    [ContractMethod(CpuFee = 1 << 14, RequiredCallFlags = CallFlags.ReadStates)]
    public bool IsContract(IReadOnlyStore snapshot, UInt160 hash)
    {
        var key = CreateStorageKey(Prefix_Contract, hash);
        return snapshot.Contains(key);
    }

    /// <summary>
    /// Maps specified ID to deployed contract.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="id">Contract ID.</param>
    /// <returns>The deployed contract.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public ContractState? GetContractById(IReadOnlyStore snapshot, int id)
    {
        var key = CreateStorageKey(Prefix_ContractHash, id);
        return snapshot.TryGet(key, out var item) ? GetContract(snapshot, new UInt160(item.Value.Span)) : null;
    }

    /// <summary>
    /// Gets hashes of all non native deployed contracts.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>Iterator with hashes of all deployed contracts.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    private StorageIterator GetContractHashes(IReadOnlyStore snapshot)
    {
        const FindOptions options = FindOptions.RemovePrefix;
        var prefixKey = CreateStorageKey(Prefix_ContractHash);
        var enumerator = snapshot.Find(prefixKey)
            .Select(p => (p.Key, p.Value, Id: BinaryPrimitives.ReadInt32BigEndian(p.Key.Key.Span[1..])))
            .Where(p => p.Id >= 0)
            .Select(p => (p.Key, p.Value))
            .GetEnumerator();
        return new StorageIterator(enumerator, 1, options);
    }

    /// <summary>
    /// Check if a method exists in a contract.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <param name="hash">The hash of the deployed contract.</param>
    /// <param name="method">The name of the method</param>
    /// <param name="pcount">The number of parameters</param>
    /// <returns>True if the method exists.</returns>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
    public bool HasMethod(IReadOnlyStore snapshot, UInt160 hash, string method, int pcount)
    {
        var contract = GetContract(snapshot, hash);
        if (contract is null) return false;
        var methodDescriptor = contract.Manifest.Abi.GetMethod(method, pcount);
        return methodDescriptor is not null;
    }

    /// <summary>
    /// Gets all deployed contracts.
    /// </summary>
    /// <param name="snapshot">The snapshot used to read data.</param>
    /// <returns>The deployed contracts.</returns>
    public IEnumerable<ContractState> ListContracts(IReadOnlyStore snapshot)
    {
        var listContractsPrefix = CreateStorageKey(Prefix_Contract);
        return snapshot.Find(listContractsPrefix).Select(kvp => kvp.Value.GetInteroperableClone<ContractState>(false));
    }

    /// <summary>
    /// Deploys a contract. It needs to pay the deployment fee and storage fee.
    /// </summary>
    /// <param name="engine">The engine used to write data.</param>
    /// <param name="nefFile">The NEF file of the contract.</param>
    /// <param name="manifest">The manifest of the contract.</param>
    /// <returns>The deployed contract.</returns>
    [ContractMethod(RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private ContractTask<ContractState> Deploy(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
    {
        return Deploy(engine, nefFile, manifest, StackItem.Null);
    }

    /// <summary>
    /// Deploys a contract. It needs to pay the deployment fee and storage fee.
    /// </summary>
    /// <param name="engine">The engine used to write data.</param>
    /// <param name="nefFile">The NEF file of the contract.</param>
    /// <param name="manifest">The manifest of the contract.</param>
    /// <param name="data">The data of the contract.</param>
    /// <returns>The deployed contract.</returns>
    [ContractMethod(RequiredCallFlags = CallFlags.All)]
    private async ContractTask<ContractState> Deploy(ApplicationEngine engine, byte[] nefFile, byte[] manifest, StackItem data)
    {
        if (engine.ScriptContainer is not Transaction tx)
            throw new InvalidOperationException();
        if (nefFile.Length == 0)
            throw new ArgumentException($"NEF file length cannot be zero.");
        if (manifest.Length == 0)
            throw new ArgumentException($"Manifest length cannot be zero.");

        engine.AddFee(Math.Max(
            engine.StoragePrice * (nefFile.Length + manifest.Length),
            GetMinimumDeploymentFee(engine.SnapshotCache)
            ));

        NefFile nef = nefFile.AsSerializable<NefFile>();
        ContractManifest parsedManifest = ContractManifest.Parse(manifest);
        Helper.Check(new Script(nef.Script, true), parsedManifest.Abi);
        UInt160 hash = Helper.GetContractHash(tx.Sender, nef.CheckSum, parsedManifest.Name);

        if (Policy.IsBlocked(engine.SnapshotCache, hash))
            throw new InvalidOperationException($"The contract {hash} has been blocked.");

        StorageKey key = CreateStorageKey(Prefix_Contract, hash);
        if (engine.SnapshotCache.Contains(key))
            throw new InvalidOperationException($"Contract Already Exists: {hash}");
        ContractState contract = new()
        {
            Id = GetNextAvailableId(engine.SnapshotCache),
            UpdateCounter = 0,
            Nef = nef,
            Hash = hash,
            Manifest = parsedManifest
        };

        if (!contract.Manifest.IsValid(engine.Limits, hash)) throw new InvalidOperationException($"Invalid Manifest: {hash}");

        engine.SnapshotCache.Add(key, StorageItem.CreateSealed(contract));
        engine.SnapshotCache.Add(CreateStorageKey(Prefix_ContractHash, contract.Id), new StorageItem(hash.ToArray()));

        await OnDeployAsync(engine, contract, data, false);

        return contract;
    }

    /// <summary>
    /// Updates a contract. It needs to pay the storage fee.
    /// </summary>
    /// <param name="engine">The engine used to write data.</param>
    /// <param name="nefFile">The NEF file of the contract.</param>
    /// <param name="manifest">The manifest of the contract.</param>
    /// <returns>The updated contract.</returns>
    [ContractMethod(RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private ContractTask Update(ApplicationEngine engine, byte[]? nefFile, byte[]? manifest)
    {
        return Update(engine, nefFile, manifest, StackItem.Null);
    }

    /// <summary>
    /// Updates a contract. It needs to pay the storage fee.
    /// </summary>
    /// <param name="engine">The engine used to write data.</param>
    /// <param name="nefFile">The NEF file of the contract.</param>
    /// <param name="manifest">The manifest of the contract.</param>
    /// <param name="data">The data of the contract.</param>
    /// <returns>The updated contract.</returns>
    [ContractMethod(RequiredCallFlags = CallFlags.All)]
    private ContractTask Update(ApplicationEngine engine, byte[]? nefFile, byte[]? manifest, StackItem data)
    {
        if (nefFile is null && manifest is null)
            throw new ArgumentException("NEF file and manifest cannot both be null.");

        engine.AddFee(engine.StoragePrice * ((nefFile?.Length ?? 0) + (manifest?.Length ?? 0)));

        var contractState = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Contract, engine.CallingScriptHash!))
            ?? throw new InvalidOperationException($"Updating Contract Does Not Exist: {engine.CallingScriptHash}");

        using var sealInterop = contractState.GetInteroperable(out ContractState contract, false);
        if (contract is null)
            throw new InvalidOperationException($"Updating Contract Does Not Exist: {engine.CallingScriptHash}");
        if (contract.UpdateCounter == ushort.MaxValue)
            throw new InvalidOperationException($"The contract reached the maximum number of updates.");

        if (nefFile != null)
        {
            if (nefFile.Length == 0)
                throw new ArgumentException($"NEF file length cannot be zero.");

            // Update nef
            contract.Nef = nefFile.AsSerializable<NefFile>();
        }
        if (manifest != null)
        {
            if (manifest.Length == 0)
                throw new ArgumentException($"Manifest length cannot be zero.");

            var manifestNew = ContractManifest.Parse(manifest);
            if (manifestNew.Name != contract.Manifest.Name)
                throw new InvalidOperationException("The name of the contract can't be changed.");
            if (!manifestNew.IsValid(engine.Limits, contract.Hash))
                throw new InvalidOperationException($"Invalid Manifest: {contract.Hash}");
            contract.Manifest = manifestNew;
        }
        Helper.Check(new Script(contract.Nef.Script, true), contract.Manifest.Abi);
        contract.UpdateCounter++; // Increase update counter
        return OnDeployAsync(engine, contract, data, true);
    }

    /// <summary>
    /// Destroys a contract.
    /// </summary>
    /// <param name="engine">The engine used to write data.</param>
    [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
    private void Destroy(ApplicationEngine engine)
    {
        UInt160 hash = engine.CallingScriptHash!;
        StorageKey ckey = CreateStorageKey(Prefix_Contract, hash);
        ContractState? contract = engine.SnapshotCache.TryGet(ckey)?.GetInteroperable<ContractState>(false);
        if (contract is null) return;
        engine.SnapshotCache.Delete(ckey);
        engine.SnapshotCache.Delete(CreateStorageKey(Prefix_ContractHash, contract.Id));
        foreach (var (key, _) in engine.SnapshotCache.Find(StorageKey.CreateSearchPrefix(contract.Id, ReadOnlySpan<byte>.Empty)))
            engine.SnapshotCache.Delete(key);
        // lock contract
        Policy.BlockAccount(engine.SnapshotCache, hash);
        // emit event
        engine.SendNotification(Hash, "Destroy", new Array(engine.ReferenceCounter) { hash.ToArray() });
    }
}
