using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Contract_Call = Register("System.Contract.Call", nameof(CallContract), 1 << 15, CallFlags.AllowCall, false);
        public static readonly InteropDescriptor System_Contract_CallEx = Register("System.Contract.CallEx", nameof(CallContractEx), 1 << 15, CallFlags.AllowCall, false);
        public static readonly InteropDescriptor System_Contract_CallNative = Register("System.Contract.CallNative", nameof(CallNativeContract), 0, CallFlags.None, false);
        public static readonly InteropDescriptor System_Contract_IsStandard = Register("System.Contract.IsStandard", nameof(IsStandardContract), 1 << 10, CallFlags.ReadStates, true);
        public static readonly InteropDescriptor System_Contract_GetCallFlags = Register("System.Contract.GetCallFlags", nameof(GetCallFlags), 1 << 10, CallFlags.None, false);
        /// <summary>
        /// Calculate corresponding account scripthash for given public key
        /// Warning: check first that input public key is valid, before creating the script.
        /// </summary>
        public static readonly InteropDescriptor System_Contract_CreateStandardAccount = Register("System.Contract.CreateStandardAccount", nameof(CreateStandardAccount), 1 << 8, CallFlags.None, true);
        public static readonly InteropDescriptor System_Contract_NativeOnPersist = Register("System.Contract.NativeOnPersist", nameof(NativeOnPersist), 0, CallFlags.WriteStates, false);
        public static readonly InteropDescriptor System_Contract_NativePostPersist = Register("System.Contract.NativePostPersist", nameof(NativePostPersist), 0, CallFlags.WriteStates, false);

        protected internal void CallContract(UInt160 contractHash, string method, Array args)
        {
            CallContractEx(contractHash, method, args, CallFlags.All);
        }

        protected internal void CallContractEx(UInt160 contractHash, string method, Array args, CallFlags callFlags)
        {
            if (method.StartsWith('_')) throw new ArgumentException($"Invalid Method Name: {method}");
            if ((callFlags & ~CallFlags.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(callFlags));
            CallContractInternal(contractHash, method, args, callFlags, ReturnTypeConvention.EnsureNotEmpty);
        }

        private void CallContractInternal(UInt160 contractHash, string method, Array args, CallFlags flags, ReturnTypeConvention convention)
        {
            ContractState contract = NativeContract.ContractManagement.GetContract(Snapshot, contractHash);
            if (contract is null) throw new InvalidOperationException($"Called Contract Does Not Exist: {contractHash}");
            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(method);
            if (md is null) throw new InvalidOperationException($"Method {method} Does Not Exist In Contract {contractHash}");

            if (md.Safe)
            {
                flags &= ~CallFlags.WriteStates;
            }
            else
            {
                ContractState currentContract = NativeContract.ContractManagement.GetContract(Snapshot, CurrentScriptHash);
                if (currentContract?.CanCall(contract, method) == false)
                    throw new InvalidOperationException($"Cannot Call Method {method} Of Contract {contractHash} From Contract {CurrentScriptHash}");
            }

            CallContractInternal(contract, md, args, flags, convention);
        }

        private void CallContractInternal(ContractState contract, ContractMethodDescriptor method, Array args, CallFlags flags, ReturnTypeConvention convention)
        {
            if (invocationCounter.TryGetValue(contract.Hash, out var counter))
            {
                invocationCounter[contract.Hash] = counter + 1;
            }
            else
            {
                invocationCounter[contract.Hash] = 1;
            }

            GetInvocationState(CurrentContext).Convention = convention;

            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            UInt160 callingScriptHash = state.ScriptHash;
            CallFlags callingFlags = state.CallFlags;

            if (args.Count != method.Parameters.Length) throw new InvalidOperationException($"Method {method.Name} Expects {method.Parameters.Length} Arguments But Receives {args.Count} Arguments");
            ExecutionContext context_new = LoadContract(contract, method.Name, flags & callingFlags, false);
            state = context_new.GetState<ExecutionContextState>();
            state.CallingScriptHash = callingScriptHash;

            if (NativeContract.IsNative(contract.Hash))
            {
                context_new.EvaluationStack.Push(args);
                context_new.EvaluationStack.Push(method.Name);
            }
            else
            {
                for (int i = args.Count - 1; i >= 0; i--)
                    context_new.EvaluationStack.Push(args[i]);
            }
        }

        protected internal void CallNativeContract(string name)
        {
            NativeContract contract = NativeContract.GetContract(name);
            if (contract is null || contract.ActiveBlockIndex > Snapshot.PersistingBlock.Index)
                throw new InvalidOperationException();
            contract.Invoke(this);
        }

        protected internal bool IsStandardContract(UInt160 hash)
        {
            ContractState contract = NativeContract.ContractManagement.GetContract(Snapshot, hash);

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

        protected internal void NativeOnPersist()
        {
            if (Trigger != TriggerType.OnPersist)
                throw new InvalidOperationException();
            foreach (NativeContract contract in NativeContract.Contracts)
                if (contract.ActiveBlockIndex <= Snapshot.PersistingBlock.Index)
                    contract.OnPersist(this);
        }

        protected internal void NativePostPersist()
        {
            if (Trigger != TriggerType.PostPersist)
                throw new InvalidOperationException();
            foreach (NativeContract contract in NativeContract.Contracts)
                if (contract.ActiveBlockIndex <= Snapshot.PersistingBlock.Index)
                    contract.PostPersist(this);
        }
    }
}
