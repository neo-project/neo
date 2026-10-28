// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractBenchmarkSuite.cs file belongs to the neo project and is free
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

#nullable enable

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Discovers native contract methods and materialises benchmark cases.
    /// </summary>
    public sealed class NativeContractBenchmarkSuite : IDisposable
    {
        private static readonly FieldInfo MethodDescriptorsField = typeof(NativeContract).GetField("_methodDescriptors", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly Type MetadataType = typeof(NativeContract).Assembly.GetType("Neo.SmartContract.Native.ContractMethodMetadata", throwOnError: true)!;
        private static readonly PropertyInfo MetadataName = MetadataType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public)!;
        private static readonly PropertyInfo MetadataHandler = MetadataType.GetProperty("Handler", BindingFlags.Instance | BindingFlags.Public)!;
        private static readonly PropertyInfo MetadataParameters = MetadataType.GetProperty("Parameters", BindingFlags.Instance | BindingFlags.Public)!;
        private static readonly PropertyInfo MetadataNeedEngine = MetadataType.GetProperty("NeedApplicationEngine", BindingFlags.Instance | BindingFlags.Public)!;
        private static readonly PropertyInfo MetadataNeedSnapshot = MetadataType.GetProperty("NeedSnapshot", BindingFlags.Instance | BindingFlags.Public)!;
        private static readonly PropertyInfo MetadataCpuFee = MetadataType.GetProperty("CpuFee", BindingFlags.Instance | BindingFlags.Public)!;
        private static readonly PropertyInfo MetadataStorageFee = MetadataType.GetProperty("StorageFee", BindingFlags.Instance | BindingFlags.Public)!;
        private static readonly PropertyInfo MetadataCallFlags = MetadataType.GetProperty("RequiredCallFlags", BindingFlags.Instance | BindingFlags.Public)!;

        private readonly NativeContractBenchmarkContext _context;
        private readonly ReadOnlyCollection<NativeContractBenchmarkCase> _cases;
        private readonly ReadOnlyCollection<string> _diagnostics;
        private readonly NativeContractArgumentGenerator _argumentGenerator = new();

        private NativeContractBenchmarkSuite(NativeContractBenchmarkContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            var discovery = DiscoverCases();
            _cases = discovery.Cases ?? new ReadOnlyCollection<NativeContractBenchmarkCase>(Array.Empty<NativeContractBenchmarkCase>());
            _diagnostics = discovery.Diagnostics ?? new ReadOnlyCollection<string>(Array.Empty<string>());
        }

        public IReadOnlyList<NativeContractBenchmarkCase> Cases => _cases;

        public IReadOnlyList<string> Diagnostics => _diagnostics;

        public NativeContractBenchmarkContext Context => _context;

        public static NativeContractBenchmarkSuite CreateDefault(string? configurationPath = null)
        {
            var configPath = configurationPath ?? "config.json";
            var protocol = ProtocolSettings.Load(configPath);
            var system = new NeoSystem(protocol, (string?)null);
            var context = new NativeContractBenchmarkContext(system, protocol);
            return new NativeContractBenchmarkSuite(context);
        }

        public NativeContractBenchmarkInvoker CreateInvoker(NativeContractBenchmarkCase benchmarkCase)
        {
            return new NativeContractBenchmarkInvoker(benchmarkCase, _context);
        }

        private (ReadOnlyCollection<NativeContractBenchmarkCase> Cases, ReadOnlyCollection<string> Diagnostics) DiscoverCases()
        {
            var cases = new List<NativeContractBenchmarkCase>();
            var diagnostics = new List<string>();
            diagnostics.AddRange(NativeContractBenchmarkOptions.Diagnostics);

            foreach (var contract in NativeContract.Contracts.OrderBy(c => c.Name, StringComparer.Ordinal))
            {
                if (MethodDescriptorsField.GetValue(contract) is not IEnumerable descriptors)
                    continue;

                foreach (var metadata in descriptors)
                {
                    if (MetadataName.GetValue(metadata) is not string methodName ||
                        MetadataHandler.GetValue(metadata) is not MethodInfo handler ||
                        MetadataParameters.GetValue(metadata) is not InteropParameterDescriptor[] parameters ||
                        MetadataNeedEngine.GetValue(metadata) is not bool requiresEngine ||
                        MetadataNeedSnapshot.GetValue(metadata) is not bool requiresSnapshot ||
                        MetadataCpuFee.GetValue(metadata) is not long cpuFee ||
                        MetadataStorageFee.GetValue(metadata) is not long storageFee ||
                        MetadataCallFlags.GetValue(metadata) is not CallFlags callFlags)
                    {
                        diagnostics.Add($"Skipped {contract.Name}: unable to read metadata for {metadata}");
                        continue;
                    }

                    foreach (var profile in NativeContractInputProfiles.Default)
                    {
                        if (!NativeContractBenchmarkOptions.IsSizeAllowed(profile.Size))
                        {
                            diagnostics.Add($"Skipped {contract.Name}.{methodName} [{profile.Name}] due to NEO_NATIVE_BENCH_SIZES filter.");
                            continue;
                        }

                        if (!_argumentGenerator.TryBuildArgumentFactory(contract, handler, parameters, profile, out var factory, out var summary, out var failure))
                        {
                            diagnostics.Add($"Skipped {contract.Name}.{methodName} [{profile.Name}] : {failure}");
                            continue;
                        }

                        var scenarioName = $"{profile.Name}";
                        var benchmarkCase = new NativeContractBenchmarkCase(
                            contract,
                            contract.Name,
                            methodName,
                            handler,
                            parameters,
                            requiresEngine,
                            requiresSnapshot,
                            profile,
                            scenarioName,
                            summary,
                            factory,
                            cpuFee,
                            storageFee,
                            callFlags);

                        if (!NativeContractBenchmarkOptions.ShouldInclude(benchmarkCase, out var reason))
                        {
                            diagnostics.Add(reason ?? $"Skipped {benchmarkCase.MethodDisplayName}: filtered by benchmark options.");
                            continue;
                        }

                        cases.Add(benchmarkCase);
                    }
                }
            }

            var finalCases = NativeContractBenchmarkOptions.ApplyLimit(cases, diagnostics);
            var readonlyCases = finalCases is List<NativeContractBenchmarkCase> list
                ? new ReadOnlyCollection<NativeContractBenchmarkCase>(list)
                : new ReadOnlyCollection<NativeContractBenchmarkCase>(finalCases.ToList());
            return (readonlyCases, new ReadOnlyCollection<string>(diagnostics));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
