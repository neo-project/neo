// Copyright (C) 2015-2024 The Neo Project.
//
// NativeContractExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.Extensions
{
    public static class NativeContractExtensions
    {
        /// <summary>
        /// Deploy a contract to the blockchain.
        /// </summary>
        /// <param name="snapshot">The snapshot used for deploying the contract.</param>
        /// <param name="sender">The address of the contract deployer.</param>
        /// <param name="nefFile">The <see cref="NefFile"/> file of the contract to be deployed.</param>
        /// <param name="manifest">The manifest of the contract to be deployed.</param>
        /// <param name="datoshi">The gas fee to spend for deploying the contract in the unit of datoshi, 1 datoshi = 1e-8 GAS.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ContractState DeployContract(this DataCache snapshot, UInt160 sender, byte[] nefFile, byte[] manifest, long datoshi = 200_00000000)
        {
            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nefFile, manifest, null);

            var engine = ApplicationEngine.Create(TriggerType.Application,
                sender != null ? new Transaction() { Signers = [new Signer() { Account = sender }], Attributes = [] } : null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings, gas: datoshi);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() != VMState.HALT)
            {
                Exception exception = engine.FaultException;
                while (exception?.InnerException != null) exception = exception.InnerException;
                throw exception ?? new InvalidOperationException();
            }

            var ret = new ContractState();
            ((IInteroperable)ret).FromStackItem(engine.ResultStack.Pop());
            return ret;
        }

        public static void UpdateContract(this DataCache snapshot, UInt160 callingScriptHash, byte[] nefFile, byte[] manifest)
        {
            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.ContractManagement.Hash, "update", nefFile, manifest, null);

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            // Fake calling script hash
            if (callingScriptHash != null)
            {
                engine.CurrentContext.GetState<ExecutionContextState>().NativeCallingScriptHash = callingScriptHash;
                engine.CurrentContext.GetState<ExecutionContextState>().ScriptHash = callingScriptHash;
            }

            if (engine.Execute() != VMState.HALT)
            {
                Exception exception = engine.FaultException;
                while (exception?.InnerException != null) exception = exception.InnerException;
                throw exception ?? new InvalidOperationException();
            }
        }

        public static void DestroyContract(this DataCache snapshot, UInt160 callingScriptHash)
        {
            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.ContractManagement.Hash, "destroy");

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            // Fake calling script hash
            if (callingScriptHash != null)
            {
                engine.CurrentContext.GetState<ExecutionContextState>().NativeCallingScriptHash = callingScriptHash;
                engine.CurrentContext.GetState<ExecutionContextState>().ScriptHash = callingScriptHash;
            }

            if (engine.Execute() != VMState.HALT)
            {
                Exception exception = engine.FaultException;
                while (exception?.InnerException != null) exception = exception.InnerException;
                throw exception ?? new InvalidOperationException();
            }
        }

        public static void AddContract(this DataCache snapshot, UInt160 hash, ContractState state)
        {
            var key = new KeyBuilder(NativeContract.ContractManagement.Id, 8).Add(hash);
            snapshot.Add(key, new StorageItem(state));
        }

        public static void DeleteContract(this DataCache snapshot, UInt160 hash)
        {
            var key = new KeyBuilder(NativeContract.ContractManagement.Id, 8).Add(hash);
            snapshot.Delete(key);
        }

        public static StackItem Call(this NativeContract contract, DataCache snapshot, string method, params ContractParameter[] args)
        {
            return Call(contract, snapshot, null, null, method, args);
        }

        public static StackItem Call(this NativeContract contract, DataCache snapshot, IVerifiable container, Block persistingBlock, string method, params ContractParameter[] args)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, container, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(contract.Hash, method, args);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() != VMState.HALT)
            {
                Exception exception = engine.FaultException;
                while (exception?.InnerException != null) exception = exception.InnerException;
                throw exception ?? new InvalidOperationException();
            }

            if (0 < engine.ResultStack.Count)
                return engine.ResultStack.Pop();
            return null;
        }
    }
}
