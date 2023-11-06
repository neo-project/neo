// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Neo.IO;
using Neo.SmartContract.Manifest;
using Neo.VM;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// The base class of all native contracts.
    /// </summary>
    public abstract class NativeContract
    {
        private class NativeContractsCache
        {
            public class NativeContractCacheEntry
            {
                public Dictionary<int, ContractMethodMetadata> Methods { get; set; }
                public byte[] Script { get; set; }
            }

            internal Dictionary<int, NativeContractCacheEntry> NativeContracts { get; set; } = new Dictionary<int, NativeContractCacheEntry>();

            public NativeContractCacheEntry GetAllowedMethods(NativeContract native, ApplicationEngine engine)
            {
                if (NativeContracts.TryGetValue(native.Id, out var value)) return value;

                uint index = engine.PersistingBlock is null ? Ledger.CurrentIndex(engine.Snapshot) : engine.PersistingBlock.Index;
                NativeContractCacheEntry methods = native.GetAllowedMethods(engine.ProtocolSettings, index);
                NativeContracts[native.Id] = methods;
                return methods;
            }
        }

        private static readonly List<NativeContract> contractsList = new();
        private static readonly Dictionary<UInt160, NativeContract> contractsDictionary = new();
        private readonly ImmutableHashSet<Hardfork> listenHardforks;
        private readonly ReadOnlyCollection<ContractMethodMetadata> methodDescriptors;
        private readonly ReadOnlyCollection<ContractEventAttribute> eventsDescriptors;
        private static int id_counter = 0;

        #region Named Native Contracts

        /// <summary>
        /// Gets the instance of the <see cref="Native.ContractManagement"/> class.
        /// </summary>
        public static ContractManagement ContractManagement { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="Native.StdLib"/> class.
        /// </summary>
        public static StdLib StdLib { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="Native.CryptoLib"/> class.
        /// </summary>
        public static CryptoLib CryptoLib { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="LedgerContract"/> class.
        /// </summary>
        public static LedgerContract Ledger { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="NeoToken"/> class.
        /// </summary>
        public static NeoToken NEO { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="GasToken"/> class.
        /// </summary>
        public static GasToken GAS { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="PolicyContract"/> class.
        /// </summary>
        public static PolicyContract Policy { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="Native.RoleManagement"/> class.
        /// </summary>
        public static RoleManagement RoleManagement { get; } = new();

        /// <summary>
        /// Gets the instance of the <see cref="OracleContract"/> class.
        /// </summary>
        public static OracleContract Oracle { get; } = new();

        #endregion

        /// <summary>
        /// Gets all native contracts.
        /// </summary>
        public static IReadOnlyCollection<NativeContract> Contracts { get; } = contractsList;

        /// <summary>
        /// The name of the native contract.
        /// </summary>
        public string Name => GetType().Name;

        /// <summary>
        /// Since Hardfork has to start having access to the native contract.
        /// </summary>
        public virtual Hardfork? ActiveIn { get; } = null;

        /// <summary>
        /// The hash of the native contract.
        /// </summary>
        public UInt160 Hash { get; }

        /// <summary>
        /// The id of the native contract.
        /// </summary>
        public int Id { get; } = --id_counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeContract"/> class.
        /// </summary>
        protected NativeContract()
        {
            this.Hash = Helper.GetContractHash(UInt160.Zero, 0, Name);

            // Reflection to get the methods

            List<ContractMethodMetadata> listMethods = new();
            foreach (MemberInfo member in GetType().GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                ContractMethodAttribute attribute = member.GetCustomAttribute<ContractMethodAttribute>();
                if (attribute is null) continue;
                listMethods.Add(new ContractMethodMetadata(member, attribute));
            }
            methodDescriptors = listMethods.OrderBy(p => p.Name, StringComparer.Ordinal).ThenBy(p => p.Parameters.Length).ToList().AsReadOnly();

            // Reflection to get the events
            eventsDescriptors =
                GetType().GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Array.Empty<Type>())?.
                GetCustomAttributes<ContractEventAttribute>().
                OrderBy(p => p.Order).ToList().AsReadOnly();

            // Calculate the initializations forks
            listenHardforks =
                methodDescriptors.Select(u => u.ActiveIn)
                .Concat(eventsDescriptors.Select(u => u.ActiveIn))
                .Concat(new Hardfork?[] { ActiveIn })
                .Where(u => u is not null)
                .OrderBy(u => (byte)u)
                .Cast<Hardfork>().ToImmutableHashSet();

            contractsList.Add(this);
            contractsDictionary.Add(Hash, this);
        }

        /// <summary>
        /// The allowed methods and his offsets.
        /// </summary>
        /// <param name="settings">The <see cref="ProtocolSettings"/> where the HardForks are configured.</param>
        /// <param name="index">Block index</param>
        /// <returns>The <see cref="NativeContractsCache"/>.</returns>
        private NativeContractsCache.NativeContractCacheEntry GetAllowedMethods(ProtocolSettings settings, uint index)
        {
            Dictionary<int, ContractMethodMetadata> methods = new();

            // Reflection to get the ContractMethods
            byte[] script;
            using (ScriptBuilder sb = new())
            {
                foreach (ContractMethodMetadata method in methodDescriptors.Where(u => u.ActiveIn is null || settings.IsHardforkEnabled(u.ActiveIn.Value, index)))
                {
                    method.Descriptor.Offset = sb.Length;
                    sb.EmitPush(0); //version
                    methods.Add(sb.Length, method);
                    sb.EmitSysCall(ApplicationEngine.System_Contract_CallNative);
                    sb.Emit(OpCode.RET);
                }
                script = sb.ToArray();
            }

            return new NativeContractsCache.NativeContractCacheEntry() { Methods = methods, Script = script };
        }

        /// <summary>
        /// The <see cref="ContractState"/> of the native contract.
        /// </summary>
        /// <param name="settings">The <see cref="ProtocolSettings"/> where the HardForks are configured.</param>
        /// <param name="index">Block index</param>
        /// <returns>The <see cref="ContractState"/>.</returns>
        internal ContractState GetContractState(ProtocolSettings settings, uint index)
        {
            // Get allowed methods and nef script
            NativeContractsCache.NativeContractCacheEntry allowedMethods = GetAllowedMethods(settings, index);

            // Compose nef file
            NefFile nef = new()
            {
                Compiler = "neo-core-v3.0",
                Source = string.Empty,
                Tokens = Array.Empty<MethodToken>(),
                Script = allowedMethods.Script
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            // Compose manifest
            ContractManifest manifest = new()
            {
                Name = Name,
                Groups = Array.Empty<ContractGroup>(),
                SupportedStandards = Array.Empty<string>(),
                Abi = new ContractAbi()
                {
                    Events = eventsDescriptors
                        .Where(u => u.ActiveIn is null || settings.IsHardforkEnabled(u.ActiveIn.Value, index))
                        .Select(p => p.Descriptor).ToArray(),
                    Methods = allowedMethods.Methods.Values
                        .Select(p => p.Descriptor).ToArray()
                },
                Permissions = new[] { ContractPermission.DefaultPermission },
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                Extra = null
            };

            OnManifestCompose(manifest);

            // Return ContractState
            return new ContractState
            {
                Id = Id,
                Nef = nef,
                Hash = Hash,
                Manifest = manifest
            };
        }

        protected virtual void OnManifestCompose(ContractManifest manifest) { }

        /// <summary>
        /// It is the initialize block
        /// </summary>
        /// <param name="settings">The <see cref="ProtocolSettings"/> where the HardForks are configured.</param>
        /// <param name="index">Block index</param>
        /// <param name="hardfork">Active hardfork</param>
        /// <returns>True if the native contract must be initialized</returns>
        internal bool IsInitializeBlock(ProtocolSettings settings, uint index, out Hardfork? hardfork)
        {
            // If is not configured, the Genesis is the a initialized block
            if (index == 0 && ActiveIn is null)
            {
                hardfork = null;
                return true;
            }

            // If is in the hardfork height, return true
            foreach (Hardfork hf in listenHardforks)
            {
                if (settings.Hardforks.TryGetValue(hf, out var activeIn) && activeIn == index)
                {
                    hardfork = hf;
                    return true;
                }
            }

            // Initialized not required
            hardfork = null;
            return false;
        }

        /// <summary>
        /// Is the native contract active
        /// </summary>
        /// <param name="settings">The <see cref="ProtocolSettings"/> where the HardForks are configured.</param>
        /// <param name="index">Block index</param>
        /// <returns>True if the native contract is active</returns>
        internal bool IsActive(ProtocolSettings settings, uint index)
        {
            if (ActiveIn is null) return true;

            if (!settings.Hardforks.TryGetValue(ActiveIn.Value, out var activeIn))
            {
                return false;
            }

            return activeIn <= index;
        }

        /// <summary>
        /// Checks whether the committee has witnessed the current transaction.
        /// </summary>
        /// <param name="engine">The <see cref="ApplicationEngine"/> that is executing the contract.</param>
        /// <returns><see langword="true"/> if the committee has witnessed the current transaction; otherwise, <see langword="false"/>.</returns>
        protected static bool CheckCommittee(ApplicationEngine engine)
        {
            UInt160 committeeMultiSigAddr = NEO.GetCommitteeAddress(engine.Snapshot);
            return engine.CheckWitnessInternal(committeeMultiSigAddr);
        }

        private protected KeyBuilder CreateStorageKey(byte prefix)
        {
            return new KeyBuilder(Id, prefix);
        }

        /// <summary>
        /// Gets the native contract with the specified hash.
        /// </summary>
        /// <param name="hash">The hash of the native contract.</param>
        /// <returns>The native contract with the specified hash.</returns>
        public static NativeContract GetContract(UInt160 hash)
        {
            contractsDictionary.TryGetValue(hash, out var contract);
            return contract;
        }

        internal async void Invoke(ApplicationEngine engine, byte version)
        {
            try
            {
                if (version != 0)
                    throw new InvalidOperationException($"The native contract of version {version} is not active.");
                // Get native contracts invocation cache
                NativeContractsCache nativeContracts = engine.GetState(() => new NativeContractsCache());
                NativeContractsCache.NativeContractCacheEntry currentAllowedMethods = nativeContracts.GetAllowedMethods(this, engine);
                // Check if the method is allowed
                ExecutionContext context = engine.CurrentContext;
                ContractMethodMetadata method = currentAllowedMethods.Methods[context.InstructionPointer];
                if (method.ActiveIn is not null && !engine.IsHardforkEnabled(method.ActiveIn.Value))
                    throw new InvalidOperationException($"Cannot call this method before hardfork {method.ActiveIn}.");
                ExecutionContextState state = context.GetState<ExecutionContextState>();
                if (!state.CallFlags.HasFlag(method.RequiredCallFlags))
                    throw new InvalidOperationException($"Cannot call this method with the flag {state.CallFlags}.");
                engine.AddGas(method.CpuFee * engine.ExecFeeFactor + method.StorageFee * engine.StoragePrice);
                List<object> parameters = new();
                if (method.NeedApplicationEngine) parameters.Add(engine);
                if (method.NeedSnapshot) parameters.Add(engine.Snapshot);
                for (int i = 0; i < method.Parameters.Length; i++)
                    parameters.Add(engine.Convert(context.EvaluationStack.Peek(i), method.Parameters[i]));
                object returnValue = method.Handler.Invoke(this, parameters.ToArray());
                if (returnValue is ContractTask task)
                {
                    await task;
                    returnValue = task.GetResult();
                }
                for (int i = 0; i < method.Parameters.Length; i++)
                {
                    context.EvaluationStack.Pop();
                }
                if (method.Handler.ReturnType != typeof(void) && method.Handler.ReturnType != typeof(ContractTask))
                {
                    context.EvaluationStack.Push(engine.Convert(returnValue));
                }
            }
            catch (Exception ex)
            {
                engine.Throw(ex);
            }
        }

        /// <summary>
        /// Determine whether the specified contract is a native contract.
        /// </summary>
        /// <param name="hash">The hash of the contract.</param>
        /// <returns><see langword="true"/> if the contract is native; otherwise, <see langword="false"/>.</returns>
        public static bool IsNative(UInt160 hash)
        {
            return contractsDictionary.ContainsKey(hash);
        }

        internal virtual ContractTask Initialize(ApplicationEngine engine, Hardfork? hardFork)
        {
            return ContractTask.CompletedTask;
        }

        internal virtual ContractTask OnPersist(ApplicationEngine engine)
        {
            return ContractTask.CompletedTask;
        }

        internal virtual ContractTask PostPersist(ApplicationEngine engine)
        {
            return ContractTask.CompletedTask;
        }
    }
}
