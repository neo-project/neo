// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Build.Core.SmartContract.Debugger;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Neo.Build.Core.Extensions.SmartContract
{
    public static class ApplicationEngineExtensions
    {
        public static VMState ExecuteScript(this ApplicationEngine engine, Script script)
        {
            engine.LoadScript(script);
            return engine.Execute();
        }

        public static ContractState GetContractState<T>(this ApplicationEngine engine)
            where T : class
        {
            var contractName = ExtractContractName(typeof(T));
            return engine.GetContractState(contractName);
        }

        public static ContractState GetContractState(this ApplicationEngine engine, string contractName)
        {
            var snapshot = engine.SnapshotCache;

            foreach (var contractState in NativeContract.ContractManagement.ListContracts(snapshot))
            {
                if (contractName.Equals(contractState.Manifest.Name))
                    return contractState;
            }

            // TODO: Make this exception it own class
            throw new NeoBuildException($"Contract '{contractName}' not found.", NeoBuildErrorCodes.Contracts.ContractNotFound);
        }

        public static HashSet<DebugStorage> GetContractStorages<T>(this ApplicationEngine engine)
            where T : class
        {
            var contractState = engine.GetContractState<T>();
            var prefix = StorageKey.CreateSearchPrefix(contractState.Id, []);
            var snapshot = engine.SnapshotCache;

            return [.. snapshot.Find(prefix).Select(s => new DebugStorage(s.Key, s.Value))];
        }

        private static string ExtractContractName(Type type)
        {
            var displayNameAttr = Attribute.GetCustomAttribute(type, typeof(DisplayNameAttribute)) as DisplayNameAttribute;
            if (displayNameAttr is not null)
                return displayNameAttr.DisplayName;

            return type.Name; // Class Name
        }
    }
}
