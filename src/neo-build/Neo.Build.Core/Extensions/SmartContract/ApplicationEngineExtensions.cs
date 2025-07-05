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
using Neo.Build.Core.Helpers;
using Neo.Build.Core.SmartContract.Debugger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Collections.Generic;
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
            var scriptHash = NeoBuildAttributeHelper.ExtractContractScriptHash(typeof(T));
            if (scriptHash != UInt160.Zero)
                return engine.GetContractState(scriptHash);
            else
            {
                var contractName = NeoBuildAttributeHelper.ExtractContractName(typeof(T));
                return engine.GetContractState(contractName);
            }
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

        public static ContractState GetContractState(this ApplicationEngine engine, int id)
        {
            var snapshot = engine.SnapshotCache;

            foreach (var contractState in NativeContract.ContractManagement.ListContracts(snapshot))
            {
                if (contractState.Id == id)
                    return contractState;
            }

            // TODO: Make this exception it own class
            throw new NeoBuildException($"Contract with Id '{id}' not found.", NeoBuildErrorCodes.Contracts.ContractNotFound);
        }

        public static ContractState GetContractState(this ApplicationEngine engine, UInt160 scriptHash)
        {
            var snapshot = engine.SnapshotCache;

            foreach (var contractState in NativeContract.ContractManagement.ListContracts(snapshot))
            {
                if (contractState.Hash == scriptHash)
                    return contractState;
            }

            // TODO: Make this exception it own class
            throw new NeoBuildException($"Contract '{scriptHash}' was not found.", NeoBuildErrorCodes.Contracts.ContractNotFound);
        }

        public static HashSet<DebugStorage> GetContractStorage<T>(this ApplicationEngine engine, byte[]? prefix = null, SeekDirection seekDirection = SeekDirection.Forward)
            where T : class
        {
            prefix ??= [];

            var contractState = engine.GetContractState<T>();
            var key = StorageKey.CreateSearchPrefix(contractState.Id, prefix);
            var snapshot = engine.SnapshotCache;

            return [.. snapshot.Find(key, seekDirection).Select(static s => new DebugStorage(s.Key, s.Value))];
        }
    }
}
