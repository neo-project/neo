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
    partial class InteropService
    {
        public static class Contract
        {
            public const int MaxLength = 1024 * 1024;

            public static readonly InteropDescriptor Create = Register("System.Contract.Create", Contract_Create, 0, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Update = Register("System.Contract.Update", Contract_Update, 0, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Destroy = Register("System.Contract.Destroy", Contract_Destroy, 0_01000000, TriggerType.Application, CallFlags.AllowModifyStates);
            public static readonly InteropDescriptor Call = Register("System.Contract.Call", Contract_Call, 0_01000000, TriggerType.System | TriggerType.Application, CallFlags.AllowCall);
            public static readonly InteropDescriptor CallEx = Register("System.Contract.CallEx", Contract_CallEx, 0_01000000, TriggerType.System | TriggerType.Application, CallFlags.AllowCall);
            public static readonly InteropDescriptor IsStandard = Register("System.Contract.IsStandard", Contract_IsStandard, 0_00030000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor GetCallFlags = Register("System.Contract.GetCallFlags", Contract_GetCallFlags, 0_00030000, TriggerType.All, CallFlags.None);

            /// <summary>
            /// Calculate corresponding account scripthash for given public key
            /// Warning: check first that input public key is valid, before creating the script.
            /// </summary>
            public static readonly InteropDescriptor CreateStandardAccount = Register("System.Contract.CreateStandardAccount", Contract_CreateStandardAccount, 0_00010000, TriggerType.All, CallFlags.None);

            private static bool Contract_GetCallFlags(ApplicationEngine engine)
            {
                var state = engine.CurrentContext.GetState<ExecutionContextState>();
                engine.Push((int)state.CallFlags);
                return true;
            }

            private static bool Contract_Create(ApplicationEngine engine)
            {
                if (!engine.TryPop(out ReadOnlySpan<byte> script)) return false;
                if (script.Length == 0 || script.Length > MaxLength) return false;

                if (!engine.TryPop(out ReadOnlySpan<byte> manifest)) return false;
                if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength) return false;

                if (!engine.AddGas(Storage.GasPerByte * (script.Length + manifest.Length))) return false;

                UInt160 hash = script.ToScriptHash();
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                if (contract != null) return false;
                contract = new ContractState
                {
                    Id = engine.Snapshot.ContractId.GetAndChange().NextId++,
                    Script = script.ToArray(),
                    Manifest = ContractManifest.Parse(manifest)
                };

                if (!contract.Manifest.IsValid(hash)) return false;

                engine.Snapshot.Contracts.Add(hash, contract);
                engine.Push(StackItem.FromInterface(contract));
                return true;
            }

            private static bool Contract_Update(ApplicationEngine engine)
            {
                if (!engine.TryPop(out StackItem item0)) return false;
                if (!engine.TryPop(out StackItem item1)) return false;

                if (!engine.AddGas(Storage.GasPerByte * (item0.GetByteLength() + item1.GetByteLength()))) return false;

                var contract = engine.Snapshot.Contracts.TryGet(engine.CurrentScriptHash);
                if (contract is null) return false;

                if (!item0.IsNull)
                {
                    ReadOnlySpan<byte> script = item0.GetSpan();
                    if (script.Length == 0 || script.Length > MaxLength) return false;
                    UInt160 hash_new = script.ToScriptHash();
                    if (hash_new.Equals(engine.CurrentScriptHash)) return false;
                    if (engine.Snapshot.Contracts.TryGet(hash_new) != null) return false;
                    contract = new ContractState
                    {
                        Id = contract.Id,
                        Script = script.ToArray(),
                        Manifest = contract.Manifest
                    };
                    contract.Manifest.Abi.Hash = hash_new;
                    engine.Snapshot.Contracts.Add(hash_new, contract);
                    engine.Snapshot.Contracts.Delete(engine.CurrentScriptHash);
                }
                if (!item1.IsNull)
                {
                    ReadOnlySpan<byte> manifest = item1.GetSpan();
                    if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength) return false;
                    contract = engine.Snapshot.Contracts.GetAndChange(contract.ScriptHash);
                    contract.Manifest = ContractManifest.Parse(manifest);
                    if (!contract.Manifest.IsValid(contract.ScriptHash)) return false;
                    if (!contract.HasStorage && engine.Snapshot.Storages.Find(BitConverter.GetBytes(contract.Id)).Any()) return false;
                }

                return true;
            }

            private static bool Contract_Destroy(ApplicationEngine engine)
            {
                UInt160 hash = engine.CurrentScriptHash;
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                if (contract == null) return true;
                engine.Snapshot.Contracts.Delete(hash);
                if (contract.HasStorage)
                    foreach (var (key, _) in engine.Snapshot.Storages.Find(BitConverter.GetBytes(contract.Id)))
                        engine.Snapshot.Storages.Delete(key);
                return true;
            }

            private static bool Contract_Call(ApplicationEngine engine)
            {
                if (!engine.TryPop(out ReadOnlySpan<byte> contractHash)) return false;
                if (!engine.TryPop(out string method)) return false;
                if (!engine.TryPop(out Array args)) return false;

                return Contract_CallEx(engine, new UInt160(contractHash), method, args, CallFlags.All);
            }

            private static bool Contract_CallEx(ApplicationEngine engine)
            {
                if (!engine.TryPop(out ReadOnlySpan<byte> contractHash)) return false;
                if (!engine.TryPop(out string method)) return false;
                if (!engine.TryPop(out Array args)) return false;
                if (!engine.TryPop(out int flagsValue)) return false;

                CallFlags flags = (CallFlags)flagsValue;
                if ((flags & ~CallFlags.All) != 0) return false;

                return Contract_CallEx(engine, new UInt160(contractHash), method, args, flags);
            }

            private static bool Contract_CallEx(ApplicationEngine engine, UInt160 contractHash, string method, Array args, CallFlags flags)
            {
                if (method.StartsWith('_')) return false;

                ContractState contract = engine.Snapshot.Contracts.TryGet(contractHash);
                if (contract is null) return false;

                ContractManifest currentManifest = engine.Snapshot.Contracts.TryGet(engine.CurrentScriptHash)?.Manifest;

                if (currentManifest != null && !currentManifest.CanCall(contract.Manifest, method))
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

                ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(method);
                if (md is null) return false;
                int rvcount = md.ReturnType == ContractParameterType.Void ? 0 : 1;
                ExecutionContext context_new = engine.LoadScript(contract.Script, rvcount);
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
                if (md != null) engine.LoadClonedContext(md.Offset);

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

            private static bool Contract_CreateStandardAccount(ApplicationEngine engine)
            {
                if (!engine.TryPop(out ReadOnlySpan<byte> pubKey)) return false;
                UInt160 scriptHash = SmartContract.Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubKey, ECCurve.Secp256r1)).ToScriptHash();
                engine.Push(scriptHash.ToArray());
                return true;
            }
        }
    }
}
