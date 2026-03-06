// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractBenchmarkCase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Represents a single benchmark scenario targeting a native contract method.
    /// </summary>
    public sealed class NativeContractBenchmarkCase
    {
        public NativeContractBenchmarkCase(
            NativeContract contract,
            string contractName,
            string methodName,
            MethodInfo handler,
            IReadOnlyList<InteropParameterDescriptor> parameters,
            bool requiresApplicationEngine,
            bool requiresSnapshot,
            NativeContractInputProfile profile,
            string scenarioName,
            string parameterSummary,
            Func<NativeContractBenchmarkContext, object[]> argumentFactory,
            long cpuFee,
            long storageFee,
            CallFlags requiredCallFlags)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            ContractName = contractName ?? throw new ArgumentNullException(nameof(contractName));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            RequiresApplicationEngine = requiresApplicationEngine;
            RequiresSnapshot = requiresSnapshot;
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            ScenarioName = scenarioName ?? throw new ArgumentNullException(nameof(scenarioName));
            ParameterSummary = parameterSummary ?? throw new ArgumentNullException(nameof(parameterSummary));
            ArgumentFactory = argumentFactory ?? throw new ArgumentNullException(nameof(argumentFactory));
            CpuFee = cpuFee;
            StorageFee = storageFee;
            RequiredCallFlags = requiredCallFlags;
        }

        public NativeContract Contract { get; }

        public string ContractName { get; }

        public string MethodName { get; }

        public string MethodDisplayName => $"{ContractName}.{MethodName}";

        public MethodInfo Handler { get; }

        public IReadOnlyList<InteropParameterDescriptor> Parameters { get; }

        public bool RequiresApplicationEngine { get; }

        public bool RequiresSnapshot { get; }

        public NativeContractInputProfile Profile { get; }

        public string ScenarioName { get; }

        public string ParameterSummary { get; }

        public Func<NativeContractBenchmarkContext, object[]> ArgumentFactory { get; }

        public long CpuFee { get; }

        public long StorageFee { get; }

        public CallFlags RequiredCallFlags { get; }

        public string UniqueId => $"{ContractName}:{MethodName}:{ScenarioName}:{Profile.Size}";

        public override string ToString() => $"{MethodDisplayName}[{ScenarioName}/{Profile.Name}]";
    }
}
