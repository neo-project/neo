using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Contract
        {
            public static readonly InteropDescriptor Create = Register("System.Contract.Create", Contract_Create, GetDeploymentPrice, TriggerType.Application);
            public static readonly InteropDescriptor Update = Register("System.Contract.Update", Contract_Update, GetDeploymentPrice, TriggerType.Application);
            public static readonly InteropDescriptor Destroy = Register("System.Contract.Destroy", Contract_Destroy, 0_01000000, TriggerType.Application);
            public static readonly InteropDescriptor Call = Register("System.Contract.Call", Contract_Call, 0_01000000, TriggerType.System | TriggerType.Application);
            public static readonly InteropDescriptor IsStandard = Register("System.Contract.IsStandard", Contract_IsStandard, 0_00030000, TriggerType.All);

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

                var contract = engine.Snapshot.Contracts.TryGet(engine.CurrentScriptHash);
                if (contract is null) return false;

                if (script.Length > 0)
                {
                    UInt160 hash_new = script.ToScriptHash();
                    if (hash_new.Equals(engine.CurrentScriptHash)) return false;
                    if (engine.Snapshot.Contracts.TryGet(hash_new) != null) return false;
                    contract = new ContractState
                    {
                        Script = script,
                        Manifest = contract.Manifest
                    };
                    contract.Manifest.Abi.Hash = hash_new;
                    engine.Snapshot.Contracts.Add(hash_new, contract);
                    if (contract.HasStorage)
                    {
                        foreach (var (key, value) in engine.Snapshot.Storages.Find(engine.CurrentScriptHash.ToArray()).ToArray())
                        {
                            engine.Snapshot.Storages.Add(new StorageKey
                            {
                                ScriptHash = hash_new,
                                Key = key.Key
                            }, new StorageItem
                            {
                                Value = value.Value,
                                IsConstant = false
                            });
                        }
                    }
                    Contract_Destroy(engine);
                }
                if (manifest.Length > 0)
                {
                    contract = engine.Snapshot.Contracts.GetAndChange(contract.ScriptHash);
                    contract.Manifest = ContractManifest.Parse(manifest);
                    if (!contract.Manifest.IsValid(contract.ScriptHash)) return false;
                    if (!contract.HasStorage && engine.Snapshot.Storages.Find(engine.CurrentScriptHash.ToArray()).Any()) return false;
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
                    foreach (var (key, _) in engine.Snapshot.Storages.Find(hash.ToArray()))
                        engine.Snapshot.Storages.Delete(key);
                return true;
            }

            private static bool Contract_Call(ApplicationEngine engine)
            {
                StackItem contractHash = engine.CurrentContext.EvaluationStack.Pop();

                ContractState contract = engine.Snapshot.Contracts.TryGet(new UInt160(contractHash.GetSpan()));
                if (contract is null) return false;

                StackItem method = engine.CurrentContext.EvaluationStack.Pop();
                StackItem args = engine.CurrentContext.EvaluationStack.Pop();
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

                UInt160 callingScriptHash = engine.CurrentScriptHash;
                ExecutionContext context_new = engine.LoadScript(contract.Script, 1);
                context_new.GetState<ExecutionContextState>().CallingScriptHash = callingScriptHash;
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
