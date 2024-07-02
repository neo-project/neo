// Copyright (C) 2015-2024 The Neo Project.
//
// ContractHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using System;
using System.Linq;

namespace Neo.Plugins.RestServer.Helpers
{
    public static class ContractHelper
    {
        public static ContractParameterDefinition[]? GetAbiEventParams(DataCache snapshot, UInt160 scriptHash, string eventName)
        {
            var contractState = NativeContract.ContractManagement.GetContract(snapshot, scriptHash);
            if (contractState == null)
                return [];
            return contractState.Manifest.Abi.Events.SingleOrDefault(s => s.Name.Equals(eventName, StringComparison.OrdinalIgnoreCase))?.Parameters;
        }

        public static bool IsNep17Supported(DataCache snapshot, UInt160 scriptHash)
        {
            var contractState = NativeContract.ContractManagement.GetContract(snapshot, scriptHash);
            if (contractState == null)
                return false;
            return IsNep17Supported(contractState);
        }

        public static bool IsNep11Supported(DataCache snapshot, UInt160 scriptHash)
        {
            var contractState = NativeContract.ContractManagement.GetContract(snapshot, scriptHash);
            if (contractState == null)
                return false;
            return IsNep11Supported(contractState);
        }

        public static bool IsNep17Supported(ContractState contractState) =>
            contractState.Manifest.SupportedStandards.Any(a => a.Equals("NEP-17"));

        public static bool IsNep11Supported(ContractState contractState) =>
            contractState.Manifest.SupportedStandards.Any(a => a.Equals("NEP-11"));

        public static ContractMethodDescriptor? GetContractMethod(DataCache snapshot, UInt160 scriptHash, string method, int pCount)
        {
            var contractState = NativeContract.ContractManagement.GetContract(snapshot, scriptHash);
            if (contractState == null)
                return null;
            return contractState.Manifest.Abi.GetMethod(method, pCount);
        }
    }
}
