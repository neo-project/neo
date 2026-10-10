// Copyright (C) 2015-2026 The Neo Project.
//
// SmartAccountModulePolicy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract.Native
{
    /// <summary>Exact module admission and bounded, unprivileged discovery for the native profile.</summary>
    internal static class SmartAccountModulePolicy
    {
        internal const long MaintenanceBudget = 250_000_000;

        internal static UInt256 GetCodeHash(ContractState contract)
        {
            ArgumentNullException.ThrowIfNull(contract);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(contract.Nef.ToArray());
            hash.AppendData([0]);
            hash.AppendData(SmartAccountCanonicalJson.Serialize(contract.Manifest.ToJson()));
            return new UInt256(hash.GetHashAndReset());
        }

        private static ContractState RequireDeployed(IReadOnlyStore snapshot, UInt160 module)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ArgumentNullException.ThrowIfNull(module);
            if (module == UInt160.Zero || module == SmartAccountProtocol.ServiceHash || NativeContract.IsNative(module) ||
                NativeContract.Policy.IsBlocked(snapshot, module))
                throw new InvalidOperationException("The module is zero, native or blocked.");
            return NativeContract.ContractManagement.GetContract(snapshot, module)
                ?? throw new InvalidOperationException("The module is not deployed.");
        }

        internal static bool IsCompositeVerifier(IReadOnlyStore snapshot, UInt160 module) =>
            RequireDeployed(snapshot, module).Manifest.Extra!["smartAccount"]!["compositeVerifier"] is JBoolean marker && marker.Value;

        private static void RequireMethod(ContractState contract, string name, ContractParameterType result,
            bool? safe, params ContractParameterType[] parameters)
        {
            var methods = contract.Manifest.Abi.Methods.Where(m => m.Name == name && m.Parameters.Length == parameters.Length).ToArray();
            if (methods.Length != 1) throw new InvalidOperationException($"Module ABI must have exactly one {name}/{parameters.Length} method.");
            var method = methods[0];
            if (method.ReturnType != result || (safe.HasValue && method.Safe != safe.Value) ||
                method.Offset < 0 || method.Offset >= contract.Script.Length ||
                !method.Parameters.Select(p => p.Type).SequenceEqual(parameters))
                throw new InvalidOperationException($"The module has an invalid {name} lifecycle ABI.");
        }

        internal static SmartAccountModuleBinding Inspect(IReadOnlyStore snapshot, UInt160 module,
            SmartAccountModuleKind kind, SmartAccountModuleBinding? expected = null)
        {
            if (kind is not (SmartAccountModuleKind.Verifier or SmartAccountModuleKind.Hook))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (expected is not null && expected.Contract != module)
                throw new InvalidOperationException("The stored module identity does not match the requested module.");
            var contract = RequireDeployed(snapshot, module);
            if (contract.Manifest.Extra?["smartAccount"] is not JObject profile ||
                profile["abiVersion"] is not JNumber version || version.Value != 2)
                throw new InvalidOperationException("The module must declare native SmartAccount ABI 2 with epoch-isolated authorization.");
            if (profile["profileDigest"] is not JString digestValue || digestValue.GetString() != AccountManagement.ParameterDigest ||
                profile["compositeVerifier"] is not JBoolean composite || (kind == SmartAccountModuleKind.Hook && composite.Value))
                throw new InvalidOperationException("The module must declare the exact native SmartAccount profile digest and composite verifier capability.");
            if (composite.Value)
            {
                RequireMethod(contract, "validateCompositeSignature", ContractParameterType.Array, false,
                    ContractParameterType.Hash160, ContractParameterType.Array);
                RequireMethod(contract, "postExecuteComposite", ContractParameterType.Void, false,
                    ContractParameterType.Hash160, ContractParameterType.Array, ContractParameterType.Any, ContractParameterType.Array);
                RequireMethod(contract, "getSignerDomains", ContractParameterType.Array, true, ContractParameterType.Hash160);
            }
            RequireMethod(contract, "supportsComposition", ContractParameterType.Boolean, true);
            RequireMethod(contract, "clearAccount", ContractParameterType.Void, false, ContractParameterType.Hash160);
            RequireMethod(contract, "postExecute", ContractParameterType.Void, null,
                ContractParameterType.Hash160, ContractParameterType.Array, ContractParameterType.Any);
            RequireMethod(contract, kind == SmartAccountModuleKind.Verifier ? "validateSignature" : "preExecute",
                kind == SmartAccountModuleKind.Verifier ? ContractParameterType.Boolean : ContractParameterType.Void,
                null, ContractParameterType.Hash160, ContractParameterType.Array);
            UInt256 digest = GetCodeHash(contract);
            if (expected is not null && expected.CodeHash != digest)
                throw new InvalidOperationException("The deployed module code identity has changed.");
            return new SmartAccountModuleBinding(module, digest);
        }

        private static void RequireSignerDomainMethod(IReadOnlyStore snapshot, UInt160 module) =>
            RequireMethod(RequireDeployed(snapshot, module), "getSignerDomains", ContractParameterType.Array, true, ContractParameterType.Hash160);

        internal static async ContractTask<bool> DiscoverAsync(ApplicationEngine engine, SmartAccountModuleBinding binding,
            SmartAccountModuleKind kind, bool requireLeaf)
        {
            ArgumentNullException.ThrowIfNull(engine);
            ArgumentNullException.ThrowIfNull(binding);
            _ = Inspect(engine.SnapshotCache, binding.Contract, kind, binding);
            var result = await engine.CallFromNativeContractWithGasLimitAsync(engine.CurrentScriptHash!, binding.Contract,
                "supportsComposition", CallFlags.ReadOnly, MaintenanceBudget);
            _ = Inspect(engine.SnapshotCache, binding.Contract, kind, binding);
            if (result is not Boolean boolean)
                throw new InvalidOperationException("Module composition discovery must return an exact Boolean.");
            bool composite = boolean.GetBoolean();
            if (kind == SmartAccountModuleKind.Verifier && composite != IsCompositeVerifier(engine.SnapshotCache, binding.Contract))
                throw new InvalidOperationException("The verifier composition marker disagrees with its declared profile capability.");
            if (requireLeaf && composite)
                throw new InvalidOperationException("Nested module composition is not supported.");
            if (!composite && kind == SmartAccountModuleKind.Verifier)
                RequireSignerDomainMethod(engine.SnapshotCache, binding.Contract);
            return composite;
        }

        internal static async ContractTask<IReadOnlyList<UInt256>> ReadSignerDomainsAsync(ApplicationEngine engine,
            UInt160 accountId, SmartAccountModuleBinding binding)
        {
            ArgumentNullException.ThrowIfNull(accountId);
            if (accountId == UInt160.Zero) throw new ArgumentException("A signer-domain query requires an account identity.", nameof(accountId));
            // This deliberately repeats leaf discovery; a caller cannot bypass the
            // no-nested-composition invariant with a stale result from another account.
            _ = await DiscoverAsync(engine, binding, SmartAccountModuleKind.Verifier, true);
            var result = await engine.CallFromNativeContractWithGasLimitAsync(engine.CurrentScriptHash!, binding.Contract,
                "getSignerDomains", CallFlags.ReadOnly, MaintenanceBudget, accountId.ToArray());
            _ = Inspect(engine.SnapshotCache, binding.Contract, SmartAccountModuleKind.Verifier, binding);
            if (result is not Array domains || result.Type != StackItemType.Array || domains.Count == 0)
                throw new InvalidOperationException("A verifier child must declare a non-empty signer-domain Array.");
            List<UInt256> copied = new(domains.Count);
            HashSet<UInt256> distinct = new();
            foreach (StackItem item in domains)
            {
                if (item is not ByteString bytes || bytes.Size != UInt256.Length)
                    throw new InvalidOperationException("Signer-domain commitments must be exact 32-byte ByteStrings.");
                var domain = new UInt256(bytes.GetSpan());
                if (!distinct.Add(domain)) throw new InvalidOperationException("A verifier child declares duplicate signer domains.");
                copied.Add(domain);
            }
            return copied.AsReadOnly();
        }
    }
}
