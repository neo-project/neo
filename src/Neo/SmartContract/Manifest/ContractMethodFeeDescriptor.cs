// Copyright (C) 2015-2025 The Neo Project.
//
// ContractMethodFeeDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Json;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract.Manifest
{
    public enum ContractMethodFeeMode : byte
    {
        Fixed = 0,
        Dynamic = 1
    }

    public sealed class ContractMethodFeeDescriptor : IInteroperable, IEquatable<ContractMethodFeeDescriptor>
    {
        public BigInteger Amount { get; set; }
        public UInt160? Beneficiary { get; set; }
        public ContractMethodFeeMode Mode { get; set; }
        public UInt160? DynamicScriptHash { get; set; }

        public void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Amount = @struct[0].GetInteger();
            Beneficiary = @struct[1].IsNull ? null : new UInt160(@struct[1].GetSpan());
            Mode = (ContractMethodFeeMode)(byte)@struct[2].GetInteger();
            DynamicScriptHash = @struct[3].IsNull ? null : new UInt160(@struct[3].GetSpan());
            Validate();
        }

        public StackItem ToStackItem(IReferenceCounter? referenceCounter)
        {
            return new Struct(referenceCounter)
            {
                Amount,
                Beneficiary?.ToArray() ?? StackItem.Null,
                (byte)Mode,
                DynamicScriptHash?.ToArray() ?? StackItem.Null
            };
        }

        public static ContractMethodFeeDescriptor FromJson(JObject json)
        {
            var mode = ParseMode(json["mode"]!.GetString());
            BigInteger amount = BigInteger.Zero;
            if (mode == ContractMethodFeeMode.Fixed)
            {
                if (json["amount"] is null)
                    throw new FormatException("Amount is required for fixed fee mode.");
                amount = ParseAmount(json["amount"]!);
            }
            else if (json["amount"] is not null)
            {
                amount = ParseAmount(json["amount"]!);
            }

            UInt160? beneficiary = null;
            if (json["beneficiary"] is JString beneficiaryJson)
                beneficiary = ParseUInt160OrAddress(beneficiaryJson.GetString());

            UInt160? dynamicScriptHash = null;
            if (json["dynamicScriptHash"] is JString dynamicJson)
                dynamicScriptHash = ParseUInt160OrAddress(dynamicJson.GetString());

            var descriptor = new ContractMethodFeeDescriptor
            {
                Amount = amount,
                Beneficiary = beneficiary,
                Mode = mode,
                DynamicScriptHash = dynamicScriptHash
            };
            descriptor.Validate();
            return descriptor;
        }

        public JObject ToJson()
        {
            var json = new JObject
            {
                ["mode"] = Mode == ContractMethodFeeMode.Fixed ? "fixed" : "dynamic"
            };

            if (Mode == ContractMethodFeeMode.Fixed || !Amount.IsZero)
                json["amount"] = Amount.ToString();
            if (Beneficiary != null)
                json["beneficiary"] = ToAddress(Beneficiary);
            if (Mode == ContractMethodFeeMode.Dynamic && DynamicScriptHash != null)
                json["dynamicScriptHash"] = DynamicScriptHash.ToString();

            return json;
        }

        public bool Equals(ContractMethodFeeDescriptor? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Amount == other.Amount
                && Equals(Beneficiary, other.Beneficiary)
                && Mode == other.Mode
                && Equals(DynamicScriptHash, other.DynamicScriptHash);
        }

        public override bool Equals(object? obj)
        {
            return obj is ContractMethodFeeDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Beneficiary, Mode, DynamicScriptHash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ContractMethodFeeDescriptor left, ContractMethodFeeDescriptor right)
        {
            if (left is null || right is null)
                return Equals(left, right);

            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ContractMethodFeeDescriptor left, ContractMethodFeeDescriptor right)
        {
            if (left is null || right is null)
                return !Equals(left, right);

            return !left.Equals(right);
        }

        private void Validate()
        {
            if (Amount.Sign < 0)
                throw new FormatException("Fee amount cannot be negative.");
            if (!Enum.IsDefined(typeof(ContractMethodFeeMode), Mode))
                throw new FormatException($"Invalid fee mode: {Mode}.");
            if (Mode == ContractMethodFeeMode.Dynamic && DynamicScriptHash is null)
                throw new FormatException("Dynamic fee requires dynamicScriptHash.");
            if (Mode == ContractMethodFeeMode.Fixed && DynamicScriptHash is not null)
                throw new FormatException("Fixed fee cannot specify dynamicScriptHash.");
        }

        private static ContractMethodFeeMode ParseMode(string mode)
        {
            return mode.ToLowerInvariant() switch
            {
                "fixed" => ContractMethodFeeMode.Fixed,
                "dynamic" => ContractMethodFeeMode.Dynamic,
                _ => throw new FormatException($"Invalid fee mode: {mode}.")
            };
        }

        private static BigInteger ParseAmount(JToken token)
        {
            return token switch
            {
                JString s => BigInteger.Parse(s.GetString()),
                JNumber n => ParseSafeInteger(n),
                _ => throw new FormatException("Fee amount must be an integer.")
            };
        }

        private static BigInteger ParseSafeInteger(JNumber number)
        {
            if (number.Value % 1 != 0)
                throw new FormatException("Fee amount must be an integer.");
            if (number.Value > JNumber.MAX_SAFE_INTEGER || number.Value < JNumber.MIN_SAFE_INTEGER)
                throw new FormatException("Fee amount exceeds JSON safe integer range.");
            return new BigInteger(number.Value);
        }

        private static UInt160 ParseUInt160OrAddress(string value)
        {
            if (UInt160.TryParse(value, out var result))
                return result;

            var data = value.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException($"Invalid address format: {value}.");
            return new UInt160(data.AsSpan(1));
        }

        private static string ToAddress(UInt160 hash)
        {
            return hash.ToAddress(ProtocolSettings.Default.AddressVersion);
        }
    }
}
