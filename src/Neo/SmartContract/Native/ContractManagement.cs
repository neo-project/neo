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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using Serilog;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract used to manage all deployed smart contracts.
    /// </summary>
    public sealed class ContractManagement : NativeContract
    {
        private static readonly ILogger _log = Log.ForContext<ContractManagement>();

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
            _log.Debug("Getting next available contract ID");
            StorageItem item = snapshot.GetAndChange(CreateStorageKey(Prefix_NextAvailableId));
            int value = (int)(BigInteger)item;
            item.Add(1);
            _log.Debug("Next available contract ID is {NextId}", value + 1);
            return value;
        }

        internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
        {
            if (hardfork == ActiveIn)
            {
                _log.Information("Initializing ContractManagement state...");
                var sw = Stopwatch.StartNew();
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_MinimumDeploymentFee), new StorageItem(10_00000000));
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_NextAvailableId), new StorageItem(1));
                sw.Stop();
                _log.Information("ContractManagement initialization finished in {DurationMs} ms", sw.ElapsedMilliseconds);
            }
            return ContractTask.CompletedTask;
        }

        private async ContractTask OnDeployAsync(ApplicationEngine engine, ContractState contract, StackItem data, bool update)
        {
            string action = update ? "Update" : "Deploy";
            _log.Information("Contract {Action}: Hash={ContractHash}, Name={ContractName}", action, contract.Hash, contract.Manifest.Name);
            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(ContractBasicMethod.Deploy, ContractBasicMethod.DeployPCount);
            if (md is not null)
            {
                _log.Debug("Calling _deploy method for contract {ContractHash}", contract.Hash);
                var sw = Stopwatch.StartNew();
                try
                {
                    await engine.CallFromNativeContractAsync(Hash, contract.Hash, md.Name, data, update);
                    sw.Stop();
                    _log.Debug("_deploy call for {ContractHash} completed in {DurationMs} ms", contract.Hash, sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _log.Error(ex, "_deploy call for {ContractHash} failed after {DurationMs} ms", contract.Hash, sw.ElapsedMilliseconds);
                    throw; // Rethrow exception after logging
                }
            }
            engine.SendNotification(Hash, update ? "Update" : "Deploy", new Array(engine.ReferenceCounter) { contract.Hash.ToArray() });
        }

        internal override async ContractTask OnPersistAsync(ApplicationEngine engine)
        {
            _log.Debug("ContractManagement OnPersist for block {BlockIndex}...", engine.PersistingBlock.Index);
            var swPersist = Stopwatch.StartNew();
            foreach (NativeContract contract in Contracts)
            {
                if (contract.IsInitializeBlock(engine.ProtocolSettings, engine.PersistingBlock.Index, out var hfs))
                {
                    _log.Information("Processing initialization/update for native contract {ContractName} ({ContractHash}) at block {BlockIndex}",
                        contract.Name, contract.Hash, engine.PersistingBlock.Index);
                    var swNative = Stopwatch.StartNew();
                    ContractState contractState = contract.GetContractState(engine.ProtocolSettings, engine.PersistingBlock.Index);
                    StorageItem state = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Contract, contract.Hash));
                    bool isNewDeploy = state is null;

                    if (isNewDeploy)
                    {
                        _log.Information("Deploying native contract {ContractName} ({ContractHash}) state", contract.Name, contract.Hash);
                        engine.SnapshotCache.Add(CreateStorageKey(Prefix_Contract, contract.Hash), new StorageItem(contractState));
                        engine.SnapshotCache.Add(CreateStorageKey(Prefix_ContractHash, contract.Id), new StorageItem(contract.Hash.ToArray()));

                        if (contract.ActiveIn is null)
                        {
                            _log.Information("Initializing native contract {ContractName} (ActiveIn=null)", contract.Name);
                            await contract.InitializeAsync(engine, null);
                        }
                    }
                    else
                    {
                        _log.Information("Updating native contract {ContractName} ({ContractHash}) state", contract.Name, contract.Hash);
                        using var sealInterop = state.GetInteroperable(out ContractState oldContract, false);
                        oldContract.UpdateCounter++;
                        oldContract.Nef = contractState.Nef;
                        oldContract.Manifest = contractState.Manifest;
                        _log.Information("Native contract {ContractName} updated to counter {UpdateCounter}", contract.Name, oldContract.UpdateCounter);
                    }

                    if (hfs?.Length > 0)
                    {
                        foreach (var hf in hfs)
                        {
                            _log.Information("Initializing native contract {ContractName} for hardfork {Hardfork}", contract.Name, hf);
                            await contract.InitializeAsync(engine, hf); // Assuming InitializeAsync has its own timing
                        }
                    }
                    swNative.Stop();
                    _log.Information("Native contract {ContractName} processing finished in {DurationMs} ms", contract.Name, swNative.ElapsedMilliseconds);

                    engine.SendNotification(Hash, isNewDeploy ? "Deploy" : "Update", new Array(engine.ReferenceCounter) { contract.Hash.ToArray() });
                }
            }
            swPersist.Stop();
            _log.Debug("ContractManagement OnPersist finished in {DurationMs} ms", swPersist.ElapsedMilliseconds);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private long GetMinimumDeploymentFee(IReadOnlyStore snapshot)
        {
            // In the unit of datoshi, 1 datoshi = 1e-8 GAS
            return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_MinimumDeploymentFee)];
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetMinimumDeploymentFee(ApplicationEngine engine, BigInteger value/* In the unit of datoshi, 1 datoshi = 1e-8 GAS*/)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            _log.Information("Setting minimum deployment fee to {Fee}", value);
            engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_MinimumDeploymentFee)).Set(value);
        }

        /// <summary>
        /// Gets the deployed contract with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the deployed contract.</param>
        /// <returns>The deployed contract.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public ContractState GetContract(IReadOnlyStore snapshot, UInt160 hash)
        {
            var key = CreateStorageKey(Prefix_Contract, hash);
            return snapshot.TryGet(key, out var item) ? item.GetInteroperable<ContractState>(false) : null;
        }

        /// <summary>
        /// Maps specified ID to deployed contract.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="id">Contract ID.</param>
        /// <returns>The deployed contract.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public ContractState GetContractById(IReadOnlyStore snapshot, int id)
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
        private IIterator GetContractHashes(DataCache snapshot)
        {
            const FindOptions options = FindOptions.RemovePrefix;
            byte[] prefix_key = CreateStorageKey(Prefix_ContractHash).ToArray();
            var enumerator = snapshot.Find(prefix_key)
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
        public IEnumerable<ContractState> ListContracts(DataCache snapshot)
        {
            byte[] listContractsPrefix = CreateStorageKey(Prefix_Contract).ToArray();
            return snapshot.Find(listContractsPrefix).Select(kvp => kvp.Value.GetInteroperable<ContractState>(false));
        }

        [ContractMethod(RequiredCallFlags = CallFlags.All)]
        private ContractTask<ContractState> Deploy(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
        {
            return Deploy(engine, nefFile, manifest, StackItem.Null);
        }

        [ContractMethod(RequiredCallFlags = CallFlags.All)]
        private async ContractTask<ContractState> Deploy(ApplicationEngine engine, byte[] nefFile, byte[] manifest, StackItem data)
        {
            if (engine.ScriptContainer is not Transaction tx)
                throw new InvalidOperationException("Deploy must be called within a Transaction context");
            if (nefFile.Length == 0)
                throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");
            if (manifest.Length == 0)
                throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");

            _log.Information("Attempting contract deployment: Sender={Sender}", tx.Sender);
            var swDeploy = Stopwatch.StartNew();

            // Calculate and add fee
            long minFee = GetMinimumDeploymentFee(engine.SnapshotCache);
            long storageFee = engine.StoragePrice * (nefFile.Length + manifest.Length);
            long fee = Math.Max(storageFee, minFee);
            _log.Information("Calculated deployment fee: {Fee} (StoragePrice={StoragePrice}, NefSize={NefSize}, ManifestSize={ManifestSize}, MinFee={MinFee})",
                fee, engine.StoragePrice, nefFile.Length, manifest.Length, minFee);
            engine.AddFee(fee);

            // Deserialize and check NEF/Manifest
            _log.Debug("Deserializing NEF ({NefSize} bytes) and Manifest ({ManifestSize} bytes)", nefFile.Length, manifest.Length);
            NefFile nef = nefFile.AsSerializable<NefFile>();
            ContractManifest parsedManifest = ContractManifest.Parse(manifest);
            // Consider adding timing/logging inside Helper.Check if it's complex
            Helper.Check(new Script(nef.Script, engine.IsHardforkEnabled(Hardfork.HF_Basilisk)), parsedManifest.Abi);
            UInt160 hash = Helper.GetContractHash(tx.Sender, nef.CheckSum, parsedManifest.Name);
            _log.Information("Calculated contract hash: {ContractHash} (Name: {ContractName})", hash, parsedManifest.Name);

            // Check policy and existing contract
            if (Policy.IsBlocked(engine.SnapshotCache, hash))
            {
                _log.Error("Deployment failed: Contract {ContractHash} is blocked", hash);
                throw new InvalidOperationException($"The contract {hash} has been blocked.");
            }
            StorageKey key = CreateStorageKey(Prefix_Contract, hash);
            if (engine.SnapshotCache.Contains(key))
            {
                _log.Error("Deployment failed: Contract {ContractHash} already exists", hash);
                throw new InvalidOperationException($"Contract Already Exists: {hash}");
            }

            // Create and validate contract state
            ContractState contract = new()
            {
                Id = GetNextAvailableId(engine.SnapshotCache), // This logs the ID
                UpdateCounter = 0,
                Nef = nef,
                Hash = hash,
                Manifest = parsedManifest
            };
            if (!contract.Manifest.IsValid(engine.Limits, hash))
            {
                _log.Error("Deployment failed: Manifest for {ContractHash} is invalid", hash);
                throw new InvalidOperationException($"Invalid Manifest: {hash}");
            }

            // Add to storage
            engine.SnapshotCache.Add(key, StorageItem.CreateSealed(contract));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_ContractHash, contract.Id), new StorageItem(hash.ToArray()));
            _log.Information("Contract {ContractHash} (ID: {ContractId}) added to snapshot cache", hash, contract.Id);

            // Call _deploy method
            await OnDeployAsync(engine, contract, data, false);

            swDeploy.Stop();
            _log.Information("Contract deployment finished for {ContractHash} in {DurationMs} ms", hash, swDeploy.ElapsedMilliseconds);
            return contract;
        }

        [ContractMethod(RequiredCallFlags = CallFlags.All)]
        private ContractTask Update(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
        {
            return Update(engine, nefFile, manifest, StackItem.Null);
        }

        [ContractMethod(RequiredCallFlags = CallFlags.All)]
        private async ContractTask Update(ApplicationEngine engine, byte[] nefFile, byte[] manifest, StackItem data)
        {
            if (nefFile is null && manifest is null)
                throw new ArgumentException("The nefFile and manifest cannot be null at the same time.");

            _log.Information("Attempting contract update for {ContractHash}", engine.CallingScriptHash);
            var swUpdate = Stopwatch.StartNew();

            // Add fee
            long fee = engine.StoragePrice * ((nefFile?.Length ?? 0) + (manifest?.Length ?? 0));
            _log.Information("Update fee calculated: {Fee} (StoragePrice={StoragePrice}, NefSize={NefSize}, ManifestSize={ManifestSize})",
                fee, engine.StoragePrice, nefFile?.Length ?? 0, manifest?.Length ?? 0);
            engine.AddFee(fee);

            // Get existing contract state
            StorageKey key = CreateStorageKey(Prefix_Contract, engine.CallingScriptHash);
            var contractStateItem = engine.SnapshotCache.GetAndChange(key)
                ?? throw new InvalidOperationException($"Updating Contract Does Not Exist: {engine.CallingScriptHash}");

            using var sealInterop = contractStateItem.GetInteroperable(out ContractState contract, false);
            // Contract null check already covered by GetAndChange
            if (contract.UpdateCounter == ushort.MaxValue)
            {
                _log.Error("Update failed for {ContractHash}: Maximum update count reached", contract.Hash);
                throw new InvalidOperationException($"The contract reached the maximum number of updates.");
            }

            // Update NEF if provided
            if (nefFile != null)
            {
                if (nefFile.Length == 0)
                    throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");

                _log.Information("Updating NEF for contract {ContractHash}", contract.Hash);
                contract.Nef = nefFile.AsSerializable<NefFile>();
            }
            // Update Manifest if provided
            if (manifest != null)
            {
                if (manifest.Length == 0)
                    throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");
                ContractManifest manifest_new = ContractManifest.Parse(manifest);
                if (manifest_new.Name != contract.Manifest.Name)
                    throw new InvalidOperationException("The name of the contract can't be changed.");
                if (!manifest_new.IsValid(engine.Limits, contract.Hash))
                {
                    _log.Error("Update failed for {ContractHash}: New manifest is invalid", contract.Hash);
                    throw new InvalidOperationException($"Invalid Manifest: {contract.Hash}");
                }
                contract.Manifest = manifest_new;
            }

            // Re-check script/manifest compatibility
            // Consider adding timing/logging inside Helper.Check if it's complex
            Helper.Check(new Script(contract.Nef.Script, engine.IsHardforkEnabled(Hardfork.HF_Basilisk)), contract.Manifest.Abi);

            contract.UpdateCounter++; // Increase update counter
            _log.Information("Contract {ContractHash} update counter incremented to {UpdateCounter}", contract.Hash, contract.UpdateCounter);

            // Call _deploy for update logic
            await OnDeployAsync(engine, contract, data, true);

            swUpdate.Stop();
            _log.Information("Contract update finished for {ContractHash} in {DurationMs} ms", contract.Hash, swUpdate.ElapsedMilliseconds);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private void Destroy(ApplicationEngine engine)
        {
            UInt160 hash = engine.CallingScriptHash;
            _log.Warning("Attempting to destroy contract {ContractHash}", hash);
            var swDestroy = Stopwatch.StartNew();
            StorageKey ckey = CreateStorageKey(Prefix_Contract, hash);
            ContractState contract = engine.SnapshotCache.TryGet(ckey)?.GetInteroperable<ContractState>(false);
            if (contract is null)
            {
                _log.Warning("Contract {ContractHash} not found for destruction", hash);
                swDestroy.Stop(); // Stop timer even if nothing done
                return;
            }
            engine.SnapshotCache.Delete(ckey);
            engine.SnapshotCache.Delete(CreateStorageKey(Prefix_ContractHash, contract.Id));
            _log.Information("Deleted contract state and ID mapping for {ContractHash} (ID: {ContractId})", hash, contract.Id);

            // Delete contract storage
            var storagePrefix = StorageKey.CreateSearchPrefix(contract.Id, ReadOnlySpan<byte>.Empty);
            int deletedStorageItems = 0;
            var swStorageDelete = Stopwatch.StartNew();
            foreach (var (key, _) in engine.SnapshotCache.Find(storagePrefix))
            {
                engine.SnapshotCache.Delete(key);
                deletedStorageItems++;
            }
            swStorageDelete.Stop();
            _log.Information("Deleted {StorageItemCount} storage items for contract {ContractHash} in {DurationMs} ms", deletedStorageItems, hash, swStorageDelete.ElapsedMilliseconds);

            // lock contract
            _log.Warning("Blocking account {ContractHash} after destruction", hash);
            Policy.BlockAccount(engine.SnapshotCache, hash);
            // emit event
            engine.SendNotification(Hash, "Destroy", new Array(engine.ReferenceCounter) { hash.ToArray() });
            swDestroy.Stop();
            _log.Warning("Contract {ContractHash} destroyed in {DurationMs} ms (Total time)", hash, swDestroy.ElapsedMilliseconds);
        }
    }
}
