using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.Call.
        /// Use it to call another contract dynamically.
        /// </summary>
        public static readonly InteropDescriptor System_Contract_Call = Register("System.Contract.Call", nameof(CallContract), 1 << 15, CallFlags.ReadStates | CallFlags.AllowCall);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.CallNative.
        /// </summary>
        /// <remarks>Note: It is for internal use only. Do not use it directly in smart contracts.</remarks>
        public static readonly InteropDescriptor System_Contract_CallNative = Register("System.Contract.CallNative", nameof(CallNativeContract), 0, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.IsStandard.
        /// Checks whether the specified contract is a standard contract.
        /// </summary>
        public static readonly InteropDescriptor System_Contract_IsStandard = Register("System.Contract.IsStandard", nameof(IsStandardContract), 1 << 10, CallFlags.ReadStates);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.GetCallFlags.
        /// Gets the <see cref="CallFlags"/> of the current context.
        /// </summary>
        public static readonly InteropDescriptor System_Contract_GetCallFlags = Register("System.Contract.GetCallFlags", nameof(GetCallFlags), 1 << 10, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.CreateStandardAccount.
        /// Calculates corresponding account scripthash for the given public key.
        /// </summary>
        public static readonly InteropDescriptor System_Contract_CreateStandardAccount = Register("System.Contract.CreateStandardAccount", nameof(CreateStandardAccount), 1 << 8, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.CreateMultisigAccount.
        /// Calculates corresponding multisig account scripthash for the given public keys.
        /// </summary>
        public static readonly InteropDescriptor System_Contract_CreateMultisigAccount = Register("System.Contract.CreateMultisigAccount", nameof(CreateMultisigAccount), 1 << 8, CallFlags.None);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.NativeOnPersist.
        /// </summary>
        /// <remarks>Note: It is for internal use only. Do not use it directly in smart contracts.</remarks>
        public static readonly InteropDescriptor System_Contract_NativeOnPersist = Register("System.Contract.NativeOnPersist", nameof(NativeOnPersist), 0, CallFlags.States);

        /// <summary>
        /// The <see cref="InteropDescriptor"/> of System.Contract.NativePostPersist.
        /// </summary>
        /// <remarks>Note: It is for internal use only. Do not use it directly in smart contracts.</remarks>
        public static readonly InteropDescriptor System_Contract_NativePostPersist = Register("System.Contract.NativePostPersist", nameof(NativePostPersist), 0, CallFlags.States);

        /// <summary>
        /// The implementation of System.Contract.Call.
        /// Use it to call another contract dynamically.
        /// </summary>
        /// <param name="contractHash">The hash of the contract to be called.</param>
        /// <param name="method">The method of the contract to be called.</param>
        /// <param name="callFlags">The <see cref="CallFlags"/> to be used to call the contract.</param>
        /// <param name="args">The arguments to be used.</param>
        protected internal void CallContract(UInt160 contractHash, string method, CallFlags callFlags, Array args)
        {
            if (method.StartsWith('_')) throw new ArgumentException($"Invalid Method Name: {method}");
            if ((callFlags & ~CallFlags.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(callFlags));

            ContractState contract = NativeContract.ContractManagement.GetContract(Snapshot, contractHash);
            if (contract is null) throw new InvalidOperationException($"Called Contract Does Not Exist: {contractHash}");
            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(method, args.Count);
            if (md is null) throw new InvalidOperationException($"Method \"{method}\" with {args.Count} parameter(s) doesn't exist in the contract {contractHash}.");
            bool hasReturnValue = md.ReturnType != ContractParameterType.Void;

            if (!hasReturnValue) CurrentContext.EvaluationStack.Push(StackItem.Null);
            CallContractInternal(contract, md, callFlags, hasReturnValue, args);
        }

        /// <summary>
        /// The implementation of System.Contract.CallNative.
        /// Calls to a native contract.
        /// </summary>
        /// <param name="version">The version of the native contract to be called.</param>
        protected internal void CallNativeContract(byte version)
        {
            NativeContract contract = NativeContract.GetContract(CurrentScriptHash);
            if (contract is null)
                throw new InvalidOperationException("It is not allowed to use \"System.Contract.CallNative\" directly.");
            uint[] updates = ProtocolSettings.NativeUpdateHistory[contract.Name];
            if (updates.Length == 0)
                throw new InvalidOperationException($"The native contract {contract.Name} is not active.");
            if (updates[0] > NativeContract.Ledger.CurrentIndex(Snapshot))
                throw new InvalidOperationException($"The native contract {contract.Name} is not active.");
            contract.Invoke(this, version);
        }

        /// <summary>
        /// The implementation of System.Contract.IsStandard.
        /// Checks whether the specified contract is a standard contract.
        /// </summary>
        /// <param name="hash">The hash of the contract.</param>
        /// <returns><see langword="true"/> if the contract is standard; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// The implementation of System.Contract.GetCallFlags.
        /// Gets the <see cref="CallFlags"/> of the current context.
        /// </summary>
        /// <returns>The <see cref="CallFlags"/> of the current context.</returns>
        protected internal CallFlags GetCallFlags()
        {
            var state = CurrentContext.GetState<ExecutionContextState>();
            return state.CallFlags;
        }

        /// <summary>
        /// The implementation of System.Contract.CreateStandardAccount.
        /// Calculates corresponding account scripthash for the given public key.
        /// </summary>
        /// <param name="pubKey">The public key of the account.</param>
        /// <returns>The hash of the account.</returns>
        internal protected static UInt160 CreateStandardAccount(ECPoint pubKey)
        {
            return Contract.CreateSignatureRedeemScript(pubKey).ToScriptHash();
        }

        /// <summary>
        /// The implementation of System.Contract.CreateMultisigAccount.
        /// Calculates corresponding multisig account scripthash for the given public keys.
        /// </summary>
        /// <param name="m">The minimum number of correct signatures that need to be provided in order for the verification to pass.</param>
        /// <param name="pubKeys">The public keys of the account.</param>
        /// <returns>The hash of the account.</returns>
        internal protected static UInt160 CreateMultisigAccount(int m, ECPoint[] pubKeys)
        {
            return Contract.CreateMultiSigRedeemScript(m, pubKeys).ToScriptHash();
        }

        /// <summary>
        /// The implementation of System.Contract.NativeOnPersist.
        /// Calls to the <see cref="NativeContract.OnPersist"/> of all native contracts.
        /// </summary>
        protected internal async void NativeOnPersist()
        {
            try
            {
                if (Trigger != TriggerType.OnPersist)
                    throw new InvalidOperationException();
                foreach (NativeContract contract in NativeContract.Contracts)
                {
                    uint[] updates = ProtocolSettings.NativeUpdateHistory[contract.Name];
                    if (updates.Length == 0) continue;
                    if (updates[0] <= PersistingBlock.Index)
                        await contract.OnPersist(this);
                }
            }
            catch (Exception ex)
            {
                Throw(ex);
            }
        }

        /// <summary>
        /// The implementation of System.Contract.NativePostPersist.
        /// Calls to the <see cref="NativeContract.PostPersist"/> of all native contracts.
        /// </summary>
        protected internal async void NativePostPersist()
        {
            try
            {
                if (Trigger != TriggerType.PostPersist)
                    throw new InvalidOperationException();
                foreach (NativeContract contract in NativeContract.Contracts)
                {
                    uint[] updates = ProtocolSettings.NativeUpdateHistory[contract.Name];
                    if (updates.Length == 0) continue;
                    if (updates[0] <= PersistingBlock.Index)
                        await contract.PostPersist(this);
                }
            }
            catch (Exception ex)
            {
                Throw(ex);
            }
        }
    }
}
