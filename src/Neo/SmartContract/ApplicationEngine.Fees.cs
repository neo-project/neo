// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngine.Fees.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        private const string FeeCalculatorMethodName = "CalculateFee";
        private const CallFlags FeeCalculatorCallFlags = CallFlags.ReadStates | CallFlags.AllowCall;
        private const long MaxDynamicFeeGas = 100_000; // 0.001 GAS, in datoshi

        private void ApplyCustomFee(ContractState contract, ContractMethodDescriptor method, IReadOnlyList<StackItem> args)
        {
            if (SuppressCustomFees)
                return;
            if (Trigger != TriggerType.Application)
                return;
            if (!IsHardforkEnabled(Hardfork.HF_Faun))
                return;

            var fee = method.Fee;
            if (fee is null)
                return;

            BigInteger amount = fee.Mode switch
            {
                ContractMethodFeeMode.Fixed => fee.Amount,
                ContractMethodFeeMode.Dynamic => QueryDynamicFee(fee.DynamicScriptHash ?? throw new InvalidOperationException("Dynamic fee calculator is missing."), method.Name, args),
                _ => throw new InvalidOperationException($"Unsupported fee mode: {fee.Mode}.")
            };

            if (amount.Sign < 0)
                throw new InvalidOperationException("Fee amount cannot be negative.");

            UInt160 beneficiary = fee.Beneficiary ?? contract.Manifest.Owner
                ?? throw new InvalidOperationException("Fee beneficiary is missing.");
            if (!ChargeFee(amount, beneficiary))
                throw new InvalidOperationException("Fee transfer failed.");
        }

        private bool ChargeFee(BigInteger amount, UInt160 beneficiary)
        {
            if (amount.Sign < 0)
                throw new InvalidOperationException("Fee amount cannot be negative.");
            if (amount.IsZero)
                return true;

            UInt160 payer = CallingScriptHash ?? (ScriptContainer as Transaction)?.Sender
                ?? throw new InvalidOperationException("Fee payer is not available.");

            if (!payer.Equals(CallingScriptHash) && !CheckWitnessInternal(payer))
                throw new InvalidOperationException("Fee payer did not witness the transaction.");

            return NativeContract.GAS.TransferInternal(this, payer, beneficiary, amount, StackItem.Null, callOnPayment: false)
                .GetAwaiter().GetResult();
        }

        private BigInteger QueryDynamicFee(UInt160 calculator, string method, IReadOnlyList<StackItem> args)
        {
            ContractState? calculatorContract = NativeContract.ContractManagement.GetContract(SnapshotCache, calculator);
            if (calculatorContract is null)
                throw new InvalidOperationException($"Fee calculator contract {calculator} does not exist.");
            if (NativeContract.Policy.IsBlocked(SnapshotCache, calculator))
                throw new InvalidOperationException($"The contract {calculator} has been blocked.");

            ContractMethodDescriptor? calculatorMethod = calculatorContract.Manifest.Abi.GetMethod(FeeCalculatorMethodName, 2);
            if (calculatorMethod is null)
                throw new InvalidOperationException($"Fee calculator method {FeeCalculatorMethodName} does not exist.");
            if (!calculatorMethod.Safe)
                throw new InvalidOperationException($"Fee calculator method {FeeCalculatorMethodName} must be safe.");

            var parameters = args.Select(p => p.ToParameter()).ToList();
            var argsParameter = new ContractParameter
            {
                Type = ContractParameterType.Array,
                Value = parameters
            };

            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(calculator, FeeCalculatorMethodName, FeeCalculatorCallFlags, method, argsParameter);

            using var feeEngine = ApplicationEngine.Create(TriggerType.Application, ScriptContainer, SnapshotCache.CloneCache(), PersistingBlock, ProtocolSettings, gas: MaxDynamicFeeGas);
            feeEngine.SuppressCustomFees = true;
            feeEngine.LoadScript(sb.ToArray());
            var state = feeEngine.Execute();

            AddFee(feeEngine.FeeConsumed * FeeFactor);

            if (state != VMState.HALT)
                throw feeEngine.FaultException ?? new InvalidOperationException("Fee calculator execution faulted.");
            if (feeEngine.ResultStack.Count != 1)
                throw new InvalidOperationException("Fee calculator must return a single integer.");

            var fee = feeEngine.ResultStack.Pop().GetInteger();
            if (fee.Sign < 0)
                throw new InvalidOperationException("Fee amount cannot be negative.");

            return fee;
        }
    }
}
