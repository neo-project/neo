#pragma warning disable IDE0051

using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public sealed class ManagementContract : NativeContract
    {
        public override int Id => 0;
        public override uint ActiveBlockIndex => 0;

        private const byte Prefix_MinimumDeploymentFee = 20;
        private const byte Prefix_NextAvailableId = 15;
        private const byte Prefix_Contract = 8;

        private int GetNextAvailableId(StoreView snapshot)
        {
            StorageItem item = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_NextAvailableId), () => new StorageItem(1));
            int value = (int)(BigInteger)item;
            item.Add(1);
            return value;
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_MinimumDeploymentFee), new StorageItem(1_00000000));
        }

        internal override void OnPersist(ApplicationEngine engine)
        {
            foreach (NativeContract contract in Contracts)
            {
                if (contract.ActiveBlockIndex != engine.Snapshot.PersistingBlock.Index)
                    continue;
                engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Contract).Add(contract.Hash), new StorageItem(new ContractState
                {
                    Id = contract.Id,
                    Script = contract.Script,
                    Hash = contract.Hash,
                    Manifest = contract.Manifest
                }));
                contract.Initialize(engine);
            }
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        private long GetMinimumDeploymentFee(StoreView snapshot)
        {
            return (long)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_MinimumDeploymentFee)];
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetMinimumDeploymentFee(ApplicationEngine engine, BigInteger value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_MinimumDeploymentFee)).Set(value);
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public ContractState GetContract(StoreView snapshot, UInt160 hash)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_Contract).Add(hash))?.GetInteroperable<ContractState>();
        }

        public IEnumerable<ContractState> ListContracts(StoreView snapshot)
        {
            byte[] listContractsPrefix = CreateStorageKey(Prefix_Contract).ToArray();
            return snapshot.Storages.Find(listContractsPrefix).Select(kvp => kvp.Value.GetInteroperable<ContractState>());
        }

        [ContractMethod(0, CallFlags.WriteStates)]
        private ContractState Deploy(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
        {
            if (!(engine.ScriptContainer is Transaction tx))
                throw new InvalidOperationException();
            if (nefFile.Length == 0)
                throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");
            if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength)
                throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");

            engine.AddGas(Math.Max(
                engine.StoragePrice * (nefFile.Length + manifest.Length),
                GetMinimumDeploymentFee(engine.Snapshot)
                ));

            NefFile nef = nefFile.AsSerializable<NefFile>();
            UInt160 hash = Helper.GetContractHash(tx.Sender, nef.Script);
            StorageKey key = CreateStorageKey(Prefix_Contract).Add(hash);
            if (engine.Snapshot.Storages.Contains(key))
                throw new InvalidOperationException($"Contract Already Exists: {hash}");
            ContractState contract = new ContractState
            {
                Id = GetNextAvailableId(engine.Snapshot),
                UpdateCounter = 0,
                Script = nef.Script,
                Hash = hash,
                Manifest = ContractManifest.Parse(manifest)
            };

            if (!contract.Manifest.IsValid(hash)) throw new InvalidOperationException($"Invalid Manifest Hash: {hash}");

            engine.Snapshot.Storages.Add(key, new StorageItem(contract));

            // Execute _deploy

            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod("_deploy");
            if (md != null)
                engine.CallFromNativeContract(Hash, hash, md.Name, false);

            return contract;
        }

        [ContractMethod(0, CallFlags.WriteStates)]
        private void Update(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
        {
            if (nefFile is null && manifest is null) throw new ArgumentException();

            engine.AddGas(engine.StoragePrice * ((nefFile?.Length ?? 0) + (manifest?.Length ?? 0)));

            var contract = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Contract).Add(engine.CallingScriptHash))?.GetInteroperable<ContractState>();
            if (contract is null) throw new InvalidOperationException($"Updating Contract Does Not Exist: {engine.CallingScriptHash}");

            if (nefFile != null)
            {
                if (nefFile.Length == 0)
                    throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");

                NefFile nef = nefFile.AsSerializable<NefFile>();

                // Update script
                contract.Script = nef.Script;
            }
            if (manifest != null)
            {
                if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength)
                    throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");
                contract.Manifest = ContractManifest.Parse(manifest);
                if (!contract.Manifest.IsValid(contract.Hash))
                    throw new InvalidOperationException($"Invalid Manifest Hash: {contract.Hash}");
            }
            contract.UpdateCounter++; // Increase update counter
            if (nefFile != null)
            {
                ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod("_deploy");
                if (md != null)
                    engine.CallFromNativeContract(Hash, contract.Hash, md.Name, true);
            }
        }

        [ContractMethod(0_01000000, CallFlags.WriteStates)]
        private void Destroy(ApplicationEngine engine)
        {
            UInt160 hash = engine.CallingScriptHash;
            StorageKey ckey = CreateStorageKey(Prefix_Contract).Add(hash);
            ContractState contract = engine.Snapshot.Storages.TryGet(ckey)?.GetInteroperable<ContractState>();
            if (contract is null) return;
            engine.Snapshot.Storages.Delete(ckey);
            foreach (var (key, _) in engine.Snapshot.Storages.Find(BitConverter.GetBytes(contract.Id)))
                engine.Snapshot.Storages.Delete(key);
        }
    }
}
