using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const int MaxContractLength = 1024 * 1024;

        [InteropService("System.Contract.Create", 0, TriggerType.Application, CallFlags.AllowModifyStates)]
        private bool Contract_Create()
        {
            if (!TryPop(out ReadOnlySpan<byte> script)) return false;
            if (script.Length == 0 || script.Length > MaxContractLength) return false;

            if (!TryPop(out ReadOnlySpan<byte> manifest)) return false;
            if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength) return false;

            if (!AddGas(StoragePrice * (script.Length + manifest.Length))) return false;

            UInt160 hash = script.ToScriptHash();
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract != null) return false;
            contract = new ContractState
            {
                Id = Snapshot.ContractId.GetAndChange().NextId++,
                Script = script.ToArray(),
                Manifest = ContractManifest.Parse(manifest)
            };

            if (!contract.Manifest.IsValid(hash)) return false;

            Snapshot.Contracts.Add(hash, contract);
            Push(StackItem.FromInterface(contract));
            return true;
        }

        [InteropService("System.Contract.Update", 0, TriggerType.Application, CallFlags.AllowModifyStates)]
        private bool Contract_Update()
        {
            if (!TryPop(out StackItem item0)) return false;
            if (!TryPop(out StackItem item1)) return false;

            if (!AddGas(StoragePrice * (item0.GetByteLength() + item1.GetByteLength()))) return false;

            var contract = Snapshot.Contracts.TryGet(CurrentScriptHash);
            if (contract is null) return false;

            if (!item0.IsNull)
            {
                ReadOnlySpan<byte> script = item0.GetSpan();
                if (script.Length == 0 || script.Length > MaxContractLength) return false;
                UInt160 hash_new = script.ToScriptHash();
                if (hash_new.Equals(CurrentScriptHash)) return false;
                if (Snapshot.Contracts.TryGet(hash_new) != null) return false;
                contract = new ContractState
                {
                    Id = contract.Id,
                    Script = script.ToArray(),
                    Manifest = contract.Manifest
                };
                contract.Manifest.Abi.Hash = hash_new;
                Snapshot.Contracts.Add(hash_new, contract);
                Snapshot.Contracts.Delete(CurrentScriptHash);
            }
            if (!item1.IsNull)
            {
                ReadOnlySpan<byte> manifest = item1.GetSpan();
                if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength) return false;
                contract = Snapshot.Contracts.GetAndChange(contract.ScriptHash);
                contract.Manifest = ContractManifest.Parse(manifest);
                if (!contract.Manifest.IsValid(contract.ScriptHash)) return false;
                if (!contract.HasStorage && Snapshot.Storages.Find(BitConverter.GetBytes(contract.Id)).Any()) return false;
            }

            return true;
        }

        [InteropService("System.Contract.Destroy", 0_01000000, TriggerType.Application, CallFlags.AllowModifyStates)]
        private bool Contract_Destroy()
        {
            UInt160 hash = CurrentScriptHash;
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null) return true;
            Snapshot.Contracts.Delete(hash);
            if (contract.HasStorage)
                foreach (var (key, _) in Snapshot.Storages.Find(BitConverter.GetBytes(contract.Id)))
                    Snapshot.Storages.Delete(key);
            return true;
        }

        [InteropService("System.Contract.Call", 0_01000000, TriggerType.System | TriggerType.Application, CallFlags.AllowCall)]
        private bool Contract_Call()
        {
            if (!TryPop(out ReadOnlySpan<byte> contractHash)) return false;
            if (!TryPop(out string method)) return false;
            if (!TryPop(out Array args)) return false;

            return Contract_CallEx(new UInt160(contractHash), method, args, CallFlags.All);
        }

        [InteropService("System.Contract.CallEx", 0_01000000, TriggerType.System | TriggerType.Application, CallFlags.AllowCall)]
        private bool Contract_CallEx()
        {
            if (!TryPop(out ReadOnlySpan<byte> contractHash)) return false;
            if (!TryPop(out string method)) return false;
            if (!TryPop(out Array args)) return false;
            if (!TryPop(out int flagsValue)) return false;

            CallFlags flags = (CallFlags)flagsValue;
            if ((flags & ~CallFlags.All) != 0) return false;

            return Contract_CallEx(new UInt160(contractHash), method, args, flags);
        }

        private bool Contract_CallEx(UInt160 contractHash, string method, Array args, CallFlags flags)
        {
            if (method.StartsWith('_')) return false;

            ContractState contract = Snapshot.Contracts.TryGet(contractHash);
            if (contract is null) return false;

            ContractManifest currentManifest = Snapshot.Contracts.TryGet(CurrentScriptHash)?.Manifest;

            if (currentManifest != null && !currentManifest.CanCall(contract.Manifest, method))
                return false;

            if (invocationCounter.TryGetValue(contract.ScriptHash, out var counter))
            {
                invocationCounter[contract.ScriptHash] = counter + 1;
            }
            else
            {
                invocationCounter[contract.ScriptHash] = 1;
            }

            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            UInt160 callingScriptHash = state.ScriptHash;
            CallFlags callingFlags = state.CallFlags;

            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(method);
            if (md is null) return false;
            int rvcount = md.ReturnType == ContractParameterType.Void ? 0 : 1;
            ExecutionContext context_new = LoadScript(contract.Script, rvcount);
            state = context_new.GetState<ExecutionContextState>();
            state.CallingScriptHash = callingScriptHash;
            state.CallFlags = flags & callingFlags;

            if (NativeContract.IsNative(contractHash))
            {
                context_new.EvaluationStack.Push(args);
                context_new.EvaluationStack.Push(method);
            }
            else
            {
                for (int i = args.Count - 1; i >= 0; i--)
                    context_new.EvaluationStack.Push(args[i]);
                context_new.InstructionPointer = md.Offset;
            }

            md = contract.Manifest.Abi.GetMethod("_initialize");
            if (md != null) LoadClonedContext(md.Offset);

            return true;
        }

        [InteropService("System.Contract.IsStandard", 0_00030000, TriggerType.All, CallFlags.None)]
        private bool Contract_IsStandard()
        {
            UInt160 hash = new UInt160(Pop().GetSpan());
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            bool isStandard = contract is null || contract.Script.IsStandardContract();
            Push(isStandard);
            return true;
        }

        [InteropService("System.Contract.GetCallFlags", 0_00030000, TriggerType.All, CallFlags.None)]
        private bool Contract_GetCallFlags()
        {
            var state = CurrentContext.GetState<ExecutionContextState>();
            Push((int)state.CallFlags);
            return true;
        }

        [InteropService("System.Contract.CreateStandardAccount", 0_00010000, TriggerType.All, CallFlags.None)]
        private bool Contract_CreateStandardAccount()
        {
            if (!TryPop(out ReadOnlySpan<byte> pubKey)) return false;
            UInt160 scriptHash = Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubKey, ECCurve.Secp256r1)).ToScriptHash();
            Push(scriptHash.ToArray());
            return true;
        }
    }
}
