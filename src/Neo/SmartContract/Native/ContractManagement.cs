// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract used to manage all deployed smart contracts.
    /// </summary>
    public sealed class ContractManagement : NativeContract
    {
        private const byte Prefix_MinimumDeploymentFee = 20;
        private const byte Prefix_NextAvailableId = 15;
        private const byte Prefix_Contract = 8;
        private const byte Prefix_ContractHash = 12;

        internal ContractManagement()
        {
            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractEventDescriptor
                {
                    Name = "Deploy",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "Hash",
                            Type = ContractParameterType.Hash160
                        }
                    }
                },
                new ContractEventDescriptor
                {
                    Name = "Update",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "Hash",
                            Type = ContractParameterType.Hash160
                        }
                    }
                },
                new ContractEventDescriptor
                {
                    Name = "Destroy",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "Hash",
                            Type = ContractParameterType.Hash160
                        }
                    }
                }
            };

            Manifest.Abi.Events = events.ToArray();
        }

        private int GetNextAvailableId(DataCache snapshot)
        {
            StorageItem item = snapshot.GetAndChange(CreateStorageKey(Prefix_NextAvailableId));
            int value = (int)(BigInteger)item;
            item.Add(1);
            return value;
        }

        internal override ContractTask Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Add(CreateStorageKey(Prefix_MinimumDeploymentFee), new StorageItem(10_00000000));
            engine.Snapshot.Add(CreateStorageKey(Prefix_NextAvailableId), new StorageItem(1));
            return ContractTask.CompletedTask;
        }

        private async ContractTask OnDeploy(ApplicationEngine engine, ContractState contract, StackItem data, bool update)
        {
            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod("_deploy", 2);
            if (md is not null)
                await engine.CallFromNativeContract(Hash, contract.Hash, md.Name, data, update);
            engine.SendNotification(Hash, update ? "Update" : "Deploy", new VM.Types.Array(engine.ReferenceCounter) { contract.Hash.ToArray() });
        }

        internal override async ContractTask OnPersist(ApplicationEngine engine)
        {
            foreach (NativeContract contract in Contracts)
            {
                if (contract.IsInitializeBlock(engine.ProtocolSettings, engine.PersistingBlock.Index))
                {
                    engine.Snapshot.Add(CreateStorageKey(Prefix_Contract).Add(contract.Hash), new StorageItem(new ContractState
                    {
                        Id = contract.Id,
                        Nef = contract.Nef,
                        Hash = contract.Hash,
                        Manifest = contract.Manifest
                    }));
                    engine.Snapshot.Add(CreateStorageKey(Prefix_ContractHash).AddBigEndian(contract.Id), new StorageItem(contract.Hash.ToArray()));
                    await contract.Initialize(engine);
                }
            }
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private long GetMinimumDeploymentFee(DataCache snapshot)
        {
            return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_MinimumDeploymentFee)];
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetMinimumDeploymentFee(ApplicationEngine engine, BigInteger value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_MinimumDeploymentFee)).Set(value);
        }

        /// <summary>
        /// Gets the deployed contract with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the deployed contract.</param>
        /// <returns>The deployed contract.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public ContractState GetContract(DataCache snapshot, UInt160 hash)
        {
            return snapshot.TryGet(CreateStorageKey(Prefix_Contract).Add(hash))?
                .GetInteroperable<ContractState>(ExecutionEngineLimits.Default with { MaxStackSize = 4096 });
        }

        /// <summary>
        /// Maps specified ID to deployed contract.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="id">Contract ID.</param>
        /// <returns>The deployed contract.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public ContractState GetContractById(DataCache snapshot, int id)
        {
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_ContractHash).AddBigEndian(id));
            if (item is null) return null;
            var hash = new UInt160(item.Value.Span);
            return GetContract(snapshot, hash);
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
        public bool HasMethod(DataCache snapshot, UInt160 hash, string method, int pcount)
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
            return snapshot.Find(listContractsPrefix)
                .Select(kvp => kvp.Value.GetInteroperable<ContractState>(ExecutionEngineLimits.Default with { MaxStackSize = 4096 }));
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
                throw new InvalidOperationException();
            if (nefFile.Length == 0)
                throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");
            if (manifest.Length == 0)
                throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");

            engine.AddGas(Math.Max(
                engine.StoragePrice * (nefFile.Length + manifest.Length),
                GetMinimumDeploymentFee(engine.Snapshot)
                ));

            NefFile nef = nefFile.AsSerializable<NefFile>();
            ContractManifest parsedManifest = ContractManifest.Parse(manifest);
            Helper.Check(new VM.Script(nef.Script, engine.IsHardforkEnabled(Hardfork.HF_Basilisk)), parsedManifest.Abi);
            UInt160 hash = Helper.GetContractHash(tx.Sender, nef.CheckSum, parsedManifest.Name);

            if (Policy.IsBlocked(engine.Snapshot, hash))
                throw new InvalidOperationException($"The contract {hash} has been blocked.");

            StorageKey key = CreateStorageKey(Prefix_Contract).Add(hash);
            if (engine.Snapshot.Contains(key))
                throw new InvalidOperationException($"Contract Already Exists: {hash}");
            ContractState contract = new()
            {
                Id = GetNextAvailableId(engine.Snapshot),
                UpdateCounter = 0,
                Nef = nef,
                Hash = hash,
                Manifest = parsedManifest
            };

            if (!contract.Manifest.IsValid(hash)) throw new InvalidOperationException($"Invalid Manifest Hash: {hash}");

            engine.Snapshot.Add(key, new StorageItem(contract));
            engine.Snapshot.Add(CreateStorageKey(Prefix_ContractHash).AddBigEndian(contract.Id), new StorageItem(hash.ToArray()));

            await OnDeploy(engine, contract, data, false);

            return contract;
        }

        [ContractMethod(RequiredCallFlags = CallFlags.All)]
        private ContractTask Update(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
        {
            return Update(engine, nefFile, manifest, StackItem.Null);
        }

        [ContractMethod(RequiredCallFlags = CallFlags.All)]
        private ContractTask Update(ApplicationEngine engine, byte[] nefFile, byte[] manifest, StackItem data)
        {
            if (nefFile is null && manifest is null) throw new ArgumentException();

            engine.AddGas(engine.StoragePrice * ((nefFile?.Length ?? 0) + (manifest?.Length ?? 0)));

            var contract = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Contract).Add(engine.CallingScriptHash))?
                .GetInteroperable<ContractState>(ExecutionEngineLimits.Default with { MaxStackSize = 4096 });
            if (contract is null) throw new InvalidOperationException($"Updating Contract Does Not Exist: {engine.CallingScriptHash}");
            if (contract.UpdateCounter == ushort.MaxValue) throw new InvalidOperationException($"The contract reached the maximum number of updates.");

            if (nefFile != null)
            {
                if (nefFile.Length == 0)
                    throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");

                // Update nef
                contract.Nef = nefFile.AsSerializable<NefFile>();
            }
            if (manifest != null)
            {
                if (manifest.Length == 0)
                    throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");
                ContractManifest manifest_new = ContractManifest.Parse(manifest);
                if (manifest_new.Name != contract.Manifest.Name)
                    throw new InvalidOperationException("The name of the contract can't be changed.");
                if (!manifest_new.IsValid(contract.Hash))
                    throw new InvalidOperationException($"Invalid Manifest Hash: {contract.Hash}");
                contract.Manifest = manifest_new;
            }
            Helper.Check(new VM.Script(contract.Nef.Script, engine.IsHardforkEnabled(Hardfork.HF_Basilisk)), contract.Manifest.Abi);
            contract.UpdateCounter++; // Increase update counter
            return OnDeploy(engine, contract, data, true);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private void Destroy(ApplicationEngine engine)
        {
            UInt160 hash = engine.CallingScriptHash;
            StorageKey ckey = CreateStorageKey(Prefix_Contract).Add(hash);
            ContractState contract = engine.Snapshot.TryGet(ckey)?
                .GetInteroperable<ContractState>(ExecutionEngineLimits.Default with { MaxStackSize = 4096 });
            if (contract is null) return;
            engine.Snapshot.Delete(ckey);
            engine.Snapshot.Delete(CreateStorageKey(Prefix_ContractHash).AddBigEndian(contract.Id));
            foreach (var (key, _) in engine.Snapshot.Find(StorageKey.CreateSearchPrefix(contract.Id, ReadOnlySpan<byte>.Empty)))
                engine.Snapshot.Delete(key);
            // lock contract
            Policy.BlockAccount(engine.Snapshot, hash);
            // emit event
            engine.SendNotification(Hash, "Destroy", new VM.Types.Array(engine.ReferenceCounter) { hash.ToArray() });
        }
    }
}
