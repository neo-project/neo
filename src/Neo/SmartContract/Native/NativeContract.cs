// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Neo.SmartContract.Native;

/// <summary>
/// The base class of all native contracts.
/// </summary>
public abstract class NativeContract
{
    private class NativeContractsCache
    {
        public record CacheEntry(Dictionary<int, ContractMethodMetadata> Methods, byte[] Script);

        internal Dictionary<int, CacheEntry> NativeContracts { get; set; } = new();

        public CacheEntry GetAllowedMethods(NativeContract native, ApplicationEngine engine)
        {
            if (NativeContracts.TryGetValue(native.Id, out var value)) return value;

            uint index = engine.PersistingBlock is null ? Ledger.CurrentIndex(engine.SnapshotCache) : engine.PersistingBlock.Index;
            CacheEntry methods = native.GetAllowedMethods(engine.ProtocolSettings.IsHardforkEnabled, index);
            NativeContracts[native.Id] = methods;
            return methods;
        }
    }

    public delegate bool IsHardforkEnabledDelegate(Hardfork hf, uint blockHeight);
    private static readonly List<NativeContract> s_contractsList = [];
    private static readonly Dictionary<UInt160, NativeContract> s_contractsDictionary = new();
    private readonly ImmutableHashSet<Hardfork> _usedHardforks;
    private readonly ReadOnlyCollection<ContractMethodMetadata> _methodDescriptors;
    private readonly ReadOnlyCollection<ContractEventAttribute> _eventsDescriptors;

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

    /// <summary>
    /// Gets the instance of the <see cref="Notary"/> class.
    /// </summary>
    public static Notary Notary { get; } = new();

    /// <summary>
    /// Gets the instance of the <see cref="Treasury"/> class.
    /// </summary>
    public static Treasury Treasury { get; } = new();

    public static TokenManagement TokenManagement { get; } = new();

    public static Governance Governance { get; } = new();

    /// <summary>
    /// Gets the instance of the <see cref="NeoToken"/> class.
    /// </summary>
    public static NeoToken NEO { get; } = new();

    #endregion

    /// <summary>
    /// Gets all native contracts.
    /// </summary>
    public static IReadOnlyCollection<NativeContract> Contracts { get; } = s_contractsList;

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
    public int Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeContract"/> class.
    /// </summary>
    protected NativeContract(int id)
    {
        Id = id;
        Hash = Helper.GetContractHash(UInt160.Zero, 0, Name);

        // Reflection to get the methods

        List<ContractMethodMetadata> listMethods = [];
        foreach (var member in GetType().GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
        {
            foreach (var attribute in member.GetCustomAttributes<ContractMethodAttribute>())
            {
                listMethods.Add(new ContractMethodMetadata(member, attribute));
            }
        }
        _methodDescriptors = listMethods.OrderBy(p => p.Name, StringComparer.Ordinal).ThenBy(p => p.Parameters.Length).ToList().AsReadOnly();

        // Reflection to get the events
        _eventsDescriptors = GetType()
            .GetCustomAttributes<ContractEventAttribute>(true)
            .OrderBy(p => p.Order)
            .ToList()
            .AsReadOnly();

        // Calculate the initializations forks
        _usedHardforks =
            _methodDescriptors.Select(u => u.ActiveIn)
                .Concat(_methodDescriptors.Select(u => u.DeprecatedIn))
                .Concat(_eventsDescriptors.Select(u => u.DeprecatedIn))
                .Concat(_eventsDescriptors.Select(u => u.ActiveIn))
                .Concat([ActiveIn])
                .Where(u => u.HasValue)
                .Select(u => u!.Value)
                .OrderBy(u => (byte)u)
                .Cast<Hardfork>().ToImmutableHashSet();
        s_contractsList.Add(this);
        s_contractsDictionary.Add(Hash, this);
    }

    /// <summary>
    /// The allowed methods and his offsets.
    /// </summary>
    /// <param name="hfChecker">Hardfork checker</param>
    /// <param name="blockHeight">Block height. Used to check the hardforks and active methods.</param>
    /// <returns>The <see cref="NativeContractsCache"/>.</returns>
    private NativeContractsCache.CacheEntry GetAllowedMethods(IsHardforkEnabledDelegate hfChecker, uint blockHeight)
    {
        Dictionary<int, ContractMethodMetadata> methods = new();

        // Reflection to get the ContractMethods
        byte[] script;
        using (ScriptBuilder sb = new())
        {
            foreach (ContractMethodMetadata method in _methodDescriptors.Where(u => IsActive(u, hfChecker, blockHeight)))
            {
                method.Descriptor.Offset = sb.Length;
                sb.EmitPush(0); //version
                methods.Add(sb.Length, method);
                sb.EmitSysCall(ApplicationEngine.System_Contract_CallNative);
                sb.Emit(OpCode.RET);
            }
            script = sb.ToArray();
        }

        return new NativeContractsCache.CacheEntry(methods, script);
    }

    /// <summary>
    /// The <see cref="ContractState"/> of the native contract.
    /// </summary>
    /// <param name="settings">The <see cref="ProtocolSettings"/> where the HardForks are configured.</param>
    /// <param name="blockHeight">Block index</param>
    /// <returns>The <see cref="ContractState"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContractState GetContractState(ProtocolSettings settings, uint blockHeight) => GetContractState(settings.IsHardforkEnabled, blockHeight);

    internal static bool IsActive(IHardforkActivable u, IsHardforkEnabledDelegate hfChecker, uint blockHeight)
    {
        return  // no hardfork is involved
                u.ActiveIn is null && u.DeprecatedIn is null ||
                // deprecated method hardfork is involved
                u.DeprecatedIn is not null && hfChecker(u.DeprecatedIn.Value, blockHeight) == false ||
                // active method hardfork is involved
                u.ActiveIn is not null && hfChecker(u.ActiveIn.Value, blockHeight);
    }

    /// <summary>
    /// The <see cref="ContractState"/> of the native contract.
    /// </summary>
    /// <param name="hfChecker">Hardfork checker</param>
    /// <param name="blockHeight">Block height. Used to check hardforks and active methods.</param>
    /// <returns>The <see cref="ContractState"/>.</returns>
    public ContractState GetContractState(IsHardforkEnabledDelegate hfChecker, uint blockHeight)
    {
        // Get allowed methods and nef script
        var allowedMethods = GetAllowedMethods(hfChecker, blockHeight);

        // Compose nef file
        var nef = new NefFile()
        {
            Compiler = "neo-core-v3.0",
            Source = string.Empty,
            Tokens = [],
            Script = allowedMethods.Script
        };
        nef.CheckSum = NefFile.ComputeChecksum(nef);

        // Compose manifest
        var manifest = new ContractManifest()
        {
            Name = Name,
            Groups = [],
            SupportedStandards = [],
            Abi = new ContractAbi
            {
                Events = _eventsDescriptors
                    .Where(u => IsActive(u, hfChecker, blockHeight))
                    .Select(p => p.Descriptor).ToArray(),
                Methods = allowedMethods.Methods.Values
                    .Select(p => p.Descriptor).ToArray()
            },
            Permissions = [ContractPermission.DefaultPermission],
            Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
            Extra = null
        };

        OnManifestCompose(hfChecker, blockHeight, manifest);

        // Return ContractState
        return new ContractState
        {
            Id = Id,
            Nef = nef,
            Hash = Hash,
            Manifest = manifest
        };
    }

    protected virtual void OnManifestCompose(IsHardforkEnabledDelegate hfChecker, uint blockHeight, ContractManifest manifest) { }

    /// <summary>
    /// It is the initialize block
    /// </summary>
    /// <param name="settings">The <see cref="ProtocolSettings"/> where the HardForks are configured.</param>
    /// <param name="index">Block index</param>
    /// <param name="hardforks">Active hardforks</param>
    /// <returns>True if the native contract must be initialized</returns>
    internal bool IsInitializeBlock(ProtocolSettings settings, uint index, [NotNullWhen(true)] out Hardfork[]? hardforks)
    {
        var hfs = new List<Hardfork>();

        // If is in the hardfork height, add them to return array
        foreach (var hf in _usedHardforks)
        {
            if (!settings.Hardforks.TryGetValue(hf, out var activeIn))
            {
                // If hf is not set in the configuration (with EnsureOmmitedHardforks applied over it), it is treated as disabled.
                continue;
            }

            if (activeIn == index)
            {
                hfs.Add(hf);
            }
        }

        // Return all initialize hardforks
        if (hfs.Count > 0)
        {
            hardforks = hfs.ToArray();
            return true;
        }

        // If is not configured, the Genesis is an initialization block.
        if (index == 0 && ActiveIn is null)
        {
            hardforks = hfs.ToArray();
            return true;
        }

        // Initialized not required
        hardforks = null;
        return false;
    }

    /// <summary>
    /// Is the native contract active
    /// </summary>
    /// <param name="settings">The <see cref="ProtocolSettings"/> where the HardForks are configured.</param>
    /// <param name="blockHeight">Block height</param>
    /// <returns>True if the native contract is active</returns>
    public bool IsActive(ProtocolSettings settings, uint blockHeight)
    {
        if (ActiveIn is null) return true;

        if (!settings.Hardforks.TryGetValue(ActiveIn.Value, out var activeIn))
        {
            // If is not set in the configuration is treated as enabled from the genesis
            activeIn = 0;
        }

        return activeIn <= blockHeight;
    }

    /// <summary>
    /// Checks whether the committee has witnessed the current transaction.
    /// </summary>
    /// <param name="engine">The <see cref="ApplicationEngine"/> that is executing the contract.</param>
    /// <returns><see langword="true"/> if the committee has witnessed the current transaction; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool CheckCommittee(ApplicationEngine engine)
    {
        var committeeMultiSigAddr = NEO.GetCommitteeAddress(engine.SnapshotCache);
        return engine.CheckWitnessInternal(committeeMultiSigAddr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void AssertCommittee(ApplicationEngine engine)
    {
        if (!CheckCommittee(engine))
            throw new InvalidOperationException("Invalid committee signature. It should be a multisig(len(committee) - (len(committee) - 1) / 2)).");
    }

    protected void Notify(ApplicationEngine engine, string eventName, params object?[] args)
    {
        engine.SendNotification(Hash, eventName, new(engine.ReferenceCounter, args.Select(engine.Convert)));
    }

    #region Storage keys

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected StorageKey CreateStorageKey(byte prefix) => new KeyBuilder(Id, prefix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected StorageKey CreateStorageKey(byte prefix, byte data) => new KeyBuilder(Id, prefix) { data };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected StorageKey CreateStorageKey<T>(byte prefix, T bigEndianKey) where T : unmanaged => new KeyBuilder(Id, prefix) { bigEndianKey };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected StorageKey CreateStorageKey(byte prefix, ReadOnlySpan<byte> content) => new KeyBuilder(Id, prefix) { content };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected StorageKey CreateStorageKey(byte prefix, params IEnumerable<ISerializableSpan> serializables)
    {
        var builder = new KeyBuilder(Id, prefix);
        foreach (var serializable in serializables)
            builder.Add(serializable);
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected StorageKey CreateStorageKey(byte prefix, UInt160 hash, int bigEndianKey)
        => new KeyBuilder(Id, prefix) { hash, bigEndianKey };

    #endregion

    /// <summary>
    /// Gets the native contract with the specified hash.
    /// </summary>
    /// <param name="hash">The hash of the native contract.</param>
    /// <returns>The native contract with the specified hash.</returns>
    public static NativeContract? GetContract(UInt160 hash)
    {
        s_contractsDictionary.TryGetValue(hash, out var contract);
        return contract;
    }

    internal Dictionary<int, ContractMethodMetadata> GetContractMethods(ApplicationEngine engine)
    {
        var nativeContracts = engine.GetState(() => new NativeContractsCache());
        var currentAllowedMethods = nativeContracts.GetAllowedMethods(this, engine);
        return currentAllowedMethods.Methods;
    }

    internal async void Invoke(ApplicationEngine engine, byte version)
    {
        try
        {
            if (version != 0)
                throw new InvalidOperationException($"The native contract of version {version} is not active.");
            // Get native contracts invocation cache
            var currentAllowedMethods = GetContractMethods(engine);
            // Check if the method is allowed
            var context = engine.CurrentContext!;
            var method = currentAllowedMethods[context.InstructionPointer];
            if (method.ActiveIn is not null && !engine.IsHardforkEnabled(method.ActiveIn.Value))
                throw new InvalidOperationException($"Cannot call this method before hardfork {method.ActiveIn}.");
            if (method.DeprecatedIn is not null && engine.IsHardforkEnabled(method.DeprecatedIn.Value))
                throw new InvalidOperationException($"Cannot call this method after hardfork {method.DeprecatedIn}.");
            var state = context.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(method.RequiredCallFlags))
                throw new InvalidOperationException($"Cannot call this method with the flag {state.CallFlags}.");
            // Check native-whitelist
            if (!Policy.IsWhitelistFeeContract(engine.SnapshotCache, Hash, method.Descriptor, out var fixedFee))
            {
                // In the unit of datoshi, 1 datoshi = 1e-8 GAS
                engine.AddFee(method.CpuFee * engine.ExecFeeFactor + method.StorageFee * engine.StoragePrice);
            }
            List<object?> parameters = new();
            if (method.NeedApplicationEngine) parameters.Add(engine);
            if (method.NeedSnapshot) parameters.Add(engine.SnapshotCache);
            for (int i = 0; i < method.Parameters.Length; i++)
                parameters.Add(engine.Convert(context.EvaluationStack.Peek(i), method.Parameters[i]));
            object? returnValue = method.Handler.Invoke(this, parameters.ToArray());
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
        return s_contractsDictionary.ContainsKey(hash);
    }

    internal virtual ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardFork)
    {
        return ContractTask.CompletedTask;
    }

    internal virtual ContractTask OnPersistAsync(ApplicationEngine engine)
    {
        return ContractTask.CompletedTask;
    }

    internal virtual ContractTask PostPersistAsync(ApplicationEngine engine)
    {
        return ContractTask.CompletedTask;
    }
}
