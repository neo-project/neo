using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const int MaxContractLength = 1024 * 1024;

        public static readonly InteropDescriptor System_Contract_Create = Register("System.Contract.Create", nameof(CreateContract), 0, CallFlags.AllowModifyStates, false);
        public static readonly InteropDescriptor System_Contract_Update = Register("System.Contract.Update", nameof(UpdateContract), 0, CallFlags.AllowModifyStates, false);
        public static readonly InteropDescriptor System_Contract_Destroy = Register("System.Contract.Destroy", nameof(DestroyContract), 0_01000000, CallFlags.AllowModifyStates, false);
        public static readonly InteropDescriptor System_Contract_MethodExists = Register("System.Contract.MethodExists", nameof(MethodExists), 0_01000000, CallFlags.AllowCall, false);
        public static readonly InteropDescriptor System_Contract_Call = Register("System.Contract.Call", nameof(CallContract), 0_01000000, CallFlags.AllowCall, false);
        public static readonly InteropDescriptor System_Contract_CallEx = Register("System.Contract.CallEx", nameof(CallContractEx), 0_01000000, CallFlags.AllowCall, false);
        public static readonly InteropDescriptor System_Contract_IsStandard = Register("System.Contract.IsStandard", nameof(IsStandardContract), 0_00030000, CallFlags.AllowStates, true);
        public static readonly InteropDescriptor System_Contract_GetCallFlags = Register("System.Contract.GetCallFlags", nameof(GetCallFlags), 0_00030000, CallFlags.None, false);
        /// <summary>
        /// Calculate corresponding account scripthash for given public key
        /// Warning: check first that input public key is valid, before creating the script.
        /// </summary>
        public static readonly InteropDescriptor System_Contract_CreateStandardAccount = Register("System.Contract.CreateStandardAccount", nameof(CreateStandardAccount), 0_00010000, CallFlags.None, true);

        protected internal void CreateContract(byte[] script, byte[] manifest)
        {
            if (script.Length == 0 || script.Length > MaxContractLength)
                throw new ArgumentException($"Invalid Script Length: {script.Length}");
            if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength)
                throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");

            AddGas(StoragePrice * (script.Length + manifest.Length));

            UInt160 hash = script.ToScriptHash();
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract != null) throw new InvalidOperationException($"Contract Already Exists: {hash}");
            contract = new ContractState
            {
                Id = Snapshot.ContractId.GetAndChange().NextId++,
                Script = script.ToArray(),
                Manifest = ContractManifest.Parse(manifest)
            };

            if (!contract.Manifest.IsValid(hash)) throw new InvalidOperationException($"Invalid Manifest Hash: {hash}");

            Snapshot.Contracts.Add(hash, contract);

            // We should push it onto the caller's stack.

            Push(Convert(contract));

            // Execute _deploy

            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod("_deploy");
            if (md != null)
                CallContractInternal(contract, md, new Array(ReferenceCounter) { false }, CallFlags.All, CheckReturnType.EnsureIsEmpty);
        }

        protected internal void UpdateContract(byte[] script, byte[] manifest)
        {
            if (script is null && manifest is null) throw new ArgumentException();

            AddGas(StoragePrice * ((script?.Length ?? 0) + (manifest?.Length ?? 0)));

            var contract = Snapshot.Contracts.TryGet(CurrentScriptHash);
            if (contract is null) throw new InvalidOperationException($"Updating Contract Does Not Exist: {CurrentScriptHash}");

            if (script != null)
            {
                if (script.Length == 0 || script.Length > MaxContractLength)
                    throw new ArgumentException($"Invalid Script Length: {script.Length}");
                UInt160 hash_new = script.ToScriptHash();
                if (hash_new.Equals(CurrentScriptHash) || Snapshot.Contracts.TryGet(hash_new) != null)
                    throw new InvalidOperationException($"Adding Contract Hash Already Exist: {hash_new}");
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
            if (manifest != null)
            {
                if (manifest.Length == 0 || manifest.Length > ContractManifest.MaxLength)
                    throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");
                contract = Snapshot.Contracts.GetAndChange(contract.ScriptHash);
                contract.Manifest = ContractManifest.Parse(manifest);
                if (!contract.Manifest.IsValid(contract.ScriptHash))
                    throw new InvalidOperationException($"Invalid Manifest Hash: {contract.ScriptHash}");
                if (!contract.HasStorage && Snapshot.Storages.Find(BitConverter.GetBytes(contract.Id)).Any())
                    throw new InvalidOperationException($"Contract Does Not Support Storage But Uses Storage");
            }
            if (script != null)
            {
                ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod("_deploy");
                if (md != null)
                    CallContractInternal(contract, md, new Array(ReferenceCounter) { true }, CallFlags.All, CheckReturnType.EnsureIsEmpty);
            }
        }

        protected internal void DestroyContract()
        {
            UInt160 hash = CurrentScriptHash;
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null) return;
            Snapshot.Contracts.Delete(hash);
            if (contract.HasStorage)
                foreach (var (key, _) in Snapshot.Storages.Find(BitConverter.GetBytes(contract.Id)))
                    Snapshot.Storages.Delete(key);
        }

        protected internal bool MethodExists(UInt160 contractHash, string method)
        {
            if (contractHash is null) return false;
            if (string.IsNullOrEmpty(method)) return false;

            ContractState contract = Snapshot.Contracts.TryGet(contractHash);
            if (contract is null) return false;

            return contract.Manifest.Abi.GetMethod(method) != null;
        }

        protected internal void CallContract(UInt160 contractHash, string method, Array args)
        {
            CallContractInternal(contractHash, method, args, CallFlags.All);
        }

        protected internal void CallContractEx(UInt160 contractHash, string method, Array args, CallFlags callFlags)
        {
            if ((callFlags & ~CallFlags.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(callFlags));
            CallContractInternal(contractHash, method, args, callFlags);
        }

        protected internal void CallContractInternal(UInt160 contractHash, string method, Array args, CallFlags flags, CheckReturnType returnType = CheckReturnType.EnsureNotEmpty)
        {
            if (method.StartsWith('_')) throw new ArgumentException($"Invalid Method Name: {method}");

            ContractState contract = Snapshot.Contracts.TryGet(contractHash);
            if (contract is null) throw new InvalidOperationException($"Called Contract Does Not Exist: {contractHash}");
            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(method);
            if (md is null) throw new InvalidOperationException($"Method {method} Does Not Exist In Contract {contractHash}");

            ContractManifest currentManifest = Snapshot.Contracts.TryGet(CurrentScriptHash)?.Manifest;
            if (currentManifest != null && !currentManifest.CanCall(contract.Manifest, method))
                throw new InvalidOperationException($"Cannot Call Method {method} Of Contract {contractHash} From Contract {CurrentScriptHash}");

            CallContractInternal(contract, md, args, flags, returnType);
        }

        private void CallContractInternal(ContractState contract, ContractMethodDescriptor method, Array args, CallFlags flags, CheckReturnType checkReturnValue)
        {
            if (invocationCounter.TryGetValue(contract.ScriptHash, out var counter))
            {
                invocationCounter[contract.ScriptHash] = counter + 1;
            }
            else
            {
                invocationCounter[contract.ScriptHash] = 1;
            }

            GetInvocationState(CurrentContext).NeedCheckReturnValue = checkReturnValue;

            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            UInt160 callingScriptHash = state.ScriptHash;
            CallFlags callingFlags = state.CallFlags;

            if (args.Count != method.Parameters.Length) throw new InvalidOperationException($"Method {method.Name} Expects {method.Parameters.Length} Arguments But Receives {args.Count} Arguments");
            ExecutionContext context_new = LoadScript(contract.Script, method.Offset);
            state = context_new.GetState<ExecutionContextState>();
            state.CallingScriptHash = callingScriptHash;
            state.CallFlags = flags & callingFlags;

            if (NativeContract.IsNative(contract.ScriptHash))
            {
                context_new.EvaluationStack.Push(args);
                context_new.EvaluationStack.Push(method.Name);
            }
            else
            {
                for (int i = args.Count - 1; i >= 0; i--)
                    context_new.EvaluationStack.Push(args[i]);
            }

            method = contract.Manifest.Abi.GetMethod("_initialize");
            if (method != null) LoadContext(context_new.Clone(method.Offset));
        }

        protected internal bool IsStandardContract(UInt160 hash)
        {
            ContractState contract = Snapshot.Contracts.TryGet(hash);

            // It's a stored contract

            if (contract != null) return contract.Script.IsStandardContract();

            // Try to find it in the transaction

            if (ScriptContainer is Transaction tx)
            {
                foreach (var witness in tx.Witnesses)
                {
                    if (witness.ScriptHash == hash)
                    {
                        return witness.VerificationScript.IsStandardContract();
                    }
                }
            }

            // It's not possible to determine if it's standard

            return false;
        }

        protected internal CallFlags GetCallFlags()
        {
            var state = CurrentContext.GetState<ExecutionContextState>();
            return state.CallFlags;
        }

        protected internal UInt160 CreateStandardAccount(ECPoint pubKey)
        {
            return Contract.CreateSignatureRedeemScript(pubKey).ToScriptHash();
        }
    }
}
