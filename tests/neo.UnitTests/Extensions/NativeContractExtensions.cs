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
        public static ContractState DeployContract(this DataCache snapshot, UInt160 sender, byte[] nefFile, byte[] manifest, long gas = 200_00000000)
        {
            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nefFile, manifest, null);

            var engine = ApplicationEngine.Create(TriggerType.Application,
                sender != null ? new Transaction() { Signers = new Signer[] { new Signer() { Account = sender } } } : null, snapshot, null, gas);
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

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(script.ToArray());

            // Fake calling script hash
            if (callingScriptHash != null)
            {
                engine.CurrentContext.GetState<ExecutionContextState>().CallingScriptHash = callingScriptHash;
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

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(script.ToArray());

            // Fake calling script hash
            if (callingScriptHash != null)
            {
                engine.CurrentContext.GetState<ExecutionContextState>().CallingScriptHash = callingScriptHash;
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
            snapshot.Add(key, new StorageItem(state, false));
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
            var engine = ApplicationEngine.Create(TriggerType.Application, container, snapshot, persistingBlock);
            var contractState = NativeContract.ContractManagement.GetContract(snapshot, contract.Hash);
            if (contractState == null) throw new InvalidOperationException();

            var script = new ScriptBuilder();

            for (var i = args.Length - 1; i >= 0; i--)
                script.EmitPush(args[i]);

            script.EmitPush(method);
            engine.LoadContract(contractState, method, CallFlags.All, contract.Manifest.Abi.GetMethod(method).ReturnType != ContractParameterType.Void);
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
