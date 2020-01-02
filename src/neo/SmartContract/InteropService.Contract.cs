using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Contract
        {
            public static readonly InteropDescriptor Create = Register("System.Contract.Create", Contract_Create, GetDeploymentPrice, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Update = Register("System.Contract.Update", Contract_Update, GetDeploymentPrice, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Destroy = Register("System.Contract.Destroy", Contract_Destroy, 0_01000000, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Call = Register("System.Contract.Call", Contract_Call, 0_01000000, TriggerType.System | TriggerType.Application, CallFlags.AllowCall);
            public static readonly InteropDescriptor CallEx = Register("System.Contract.CallEx", Contract_CallEx, 0_01000000, TriggerType.System | TriggerType.Application, CallFlags.AllowCall);
            public static readonly InteropDescriptor IsStandard = Register("System.Contract.IsStandard", Contract_IsStandard, 0_00030000, TriggerType.All, CallFlags.None);

            private static long GetDeploymentPrice(EvaluationStack stack)
            {
                int size = stack.Peek(0).GetByteLength() + stack.Peek(1).GetByteLength();
                return Storage.GasPerByte * size;
            }

            private static bool Contract_Create(ApplicationEngine engine)
            {
                byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                if (script.Length > 1024 * 1024) return false;

                var manifest = engine.CurrentContext.EvaluationStack.Pop().GetString();
                if (manifest.Length > ContractManifest.MaxLength) return false;

                UInt160 hash = script.ToScriptHash();
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                if (contract != null) return false;
                contract = new ContractState
                {
                    Script = script,
                    Manifest = ContractManifest.Parse(manifest)
                };
                if (!contract.Manifest.IsValid(hash)) return false;

                engine.Snapshot.Contracts.Add(hash, contract);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(contract));
                return true;
            }

            private static bool Contract_Update(ApplicationEngine engine)
            {
                byte[] script = engine.CurrentContext.EvaluationStack.Pop().GetSpan().ToArray();
                if (script.Length > 1024 * 1024) return false;
                var manifest = engine.CurrentContext.EvaluationStack.Pop().GetString();
                if (manifest.Length > ContractManifest.MaxLength) return false;

                var oldcontract = engine.Snapshot.Contracts.TryGet(engine.CurrentScriptHash);
                if (oldcontract is null|| oldcontract.IsUpdated) return false;
                ContractState newcontract = null;
                if (script.Length > 0)
                {
                    UInt160 hash_new = script.ToScriptHash();
                    if (hash_new.Equals(engine.CurrentScriptHash)) return false;
                    if (engine.Snapshot.Contracts.TryGet(hash_new) != null) return false;
                    newcontract = new ContractState
                    {
                        Script = script,
                        Manifest = oldcontract.Manifest
                    };
                    if (oldcontract.RedirectionHash.Equals(UInt160.Zero))
                    {
                        newcontract.RedirectionHash = oldcontract.ScriptHash;
                    }
                    else
                    {
                        newcontract.RedirectionHash = oldcontract.RedirectionHash;
                    }
                    newcontract.IsUpdated = false;
                    newcontract.Manifest.Abi.Hash = hash_new;
                    engine.Snapshot.Contracts.Add(hash_new, newcontract);
                    if (oldcontract.RedirectionHash.Equals(UInt160.Zero))
                    {
                        oldcontract = engine.Snapshot.Contracts.GetAndChange(oldcontract.ScriptHash);
                        oldcontract.IsUpdated = true;
                    }
                    else
                    {
                        Contract_UnAppend_Destroy(engine);
                    }
                }
                if (manifest.Length > 0)
                {
                    newcontract = engine.Snapshot.Contracts.GetAndChange(newcontract.ScriptHash);
                    newcontract.Manifest = ContractManifest.Parse(manifest);
                    if (!newcontract.Manifest.IsValid(newcontract.ScriptHash)) return false;
                    if (!newcontract.HasStorage && engine.Snapshot.Storages.Find(engine.CurrentScriptHash.ToArray()).Any()) return false;
                }

                return true;
            }

            private static bool Contract_Destroy(ApplicationEngine engine)
            {
                UInt160 hash = engine.CurrentScriptHash;
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                if (contract == null) return true;
                if (!contract.RedirectionHash.Equals(UInt160.Zero))
                {
                    ContractState initcontract = engine.Snapshot.Contracts.TryGet(contract.RedirectionHash);
                    if (initcontract != null)
                    {
                        engine.Snapshot.Contracts.Delete(contract.RedirectionHash);
                        if (initcontract.HasStorage)
                            foreach (var (key, _) in engine.Snapshot.Storages.Find(contract.RedirectionHash.ToArray()))
                                engine.Snapshot.Storages.Delete(key);
                    }
                }
                engine.Snapshot.Contracts.Delete(hash);
                if (contract.HasStorage)
                    foreach (var (key, _) in engine.Snapshot.Storages.Find(hash.ToArray()))
                        engine.Snapshot.Storages.Delete(key);
                return true;
            }

            private static bool Contract_UnAppend_Destroy(ApplicationEngine engine)
            {
                UInt160 hash = engine.CurrentScriptHash;
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                if (contract == null) return true;
                engine.Snapshot.Contracts.Delete(hash);
                if (contract.HasStorage)
                    foreach (var (key, _) in engine.Snapshot.Storages.Find(hash.ToArray()))
                        engine.Snapshot.Storages.Delete(key);
                return true;
            }

            private static bool Contract_Call(ApplicationEngine engine)
            {
                StackItem contractHash = engine.CurrentContext.EvaluationStack.Pop();
                StackItem method = engine.CurrentContext.EvaluationStack.Pop();
                StackItem args = engine.CurrentContext.EvaluationStack.Pop();

                return Contract_CallEx(engine, new UInt160(contractHash.GetSpan()), method, args, CallFlags.All);
            }

            private static bool Contract_CallEx(ApplicationEngine engine)
            {
                StackItem contractHash = engine.CurrentContext.EvaluationStack.Pop();
                StackItem method = engine.CurrentContext.EvaluationStack.Pop();
                StackItem args = engine.CurrentContext.EvaluationStack.Pop();

                if (!engine.CurrentContext.EvaluationStack.TryPop<PrimitiveType>(out var flagItem))
                {
                    return false;
                }

                CallFlags flags = (CallFlags)(int)flagItem.ToBigInteger();
                if (!Enum.IsDefined(typeof(CallFlags), flags)) return false;

                return Contract_CallEx(engine, new UInt160(contractHash.GetSpan()), method, args, flags);
            }

            private static bool Contract_CallEx(ApplicationEngine engine, UInt160 contractHash, StackItem method, StackItem args, CallFlags flags)
            {
                ContractState contract = engine.Snapshot.Contracts.TryGet(contractHash);
                if (contract is null || contract.IsUpdated) return false;

                ContractManifest currentManifest = engine.Snapshot.Contracts.TryGet(engine.CurrentScriptHash)?.Manifest;

                if (currentManifest != null && !currentManifest.CanCall(contract.Manifest, method.GetString()))
                    return false;

                if (engine.InvocationCounter.TryGetValue(contract.ScriptHash, out var counter))
                {
                    engine.InvocationCounter[contract.ScriptHash] = counter + 1;
                }
                else
                {
                    engine.InvocationCounter[contract.ScriptHash] = 1;
                }

                ExecutionContextState state = engine.CurrentContext.GetState<ExecutionContextState>();
                UInt160 callingScriptHash = state.ScriptHash;
                CallFlags callingFlags = state.CallFlags;

                ExecutionContext context_new = engine.LoadScript(contract.Script, 1);
                state = context_new.GetState<ExecutionContextState>();
                state.CallingScriptHash = callingScriptHash;
                state.CallFlags = flags & callingFlags;

                context_new.EvaluationStack.Push(args);
                context_new.EvaluationStack.Push(method);
                return true;
            }

            private static bool Contract_IsStandard(ApplicationEngine engine)
            {
                UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetSpan());
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                bool isStandard = contract is null || contract.Script.IsStandardContract();
                engine.CurrentContext.EvaluationStack.Push(isStandard);
                return true;
            }
        }
    }
}
