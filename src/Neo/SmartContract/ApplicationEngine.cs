// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngine.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Array = System.Array;
using Buffer = Neo.VM.Types.Buffer;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    /// <summary>
    /// A virtual machine used to execute smart contracts in the NEO system.
    /// </summary>
    public partial class ApplicationEngine : ExecutionEngine
    {
        protected static readonly JumpTable DefaultJumpTable = ComposeDefaultJumpTable();
        protected static readonly JumpTable NotEchidnaJumpTable = ComposeNotEchidnaJumpTable();
        protected static readonly JumpTable NotFaunJumpTable = ComposeNotFaunJumpTable();

        /// <summary>
        /// The maximum cost that can be spent when a contract is executed in test mode.
        /// In the unit of datoshi, 1 datoshi = 1e-8 GAS
        /// </summary>
        public const long TestModeGas = 20_00000000;

        public delegate void OnInstanceHandlerEvent(ApplicationEngine engine);
        public delegate void OnLogEvent(ApplicationEngine engine, LogEventArgs args);
        public delegate void OnNotifyEvent(ApplicationEngine engine, NotifyEventArgs args);

        /// <summary>
        /// Triggered when a contract calls System.Runtime.Notify.
        /// </summary>
        public event OnNotifyEvent? Notify;

        /// <summary>
        /// Triggered when a contract calls System.Runtime.Log.
        /// </summary>
        public event OnLogEvent? Log;

        /// <summary>
        /// On Application Engine
        /// </summary>
        public static OnInstanceHandlerEvent? InstanceHandler;

        private static Dictionary<uint, InteropDescriptor>? services;
        // Total amount of GAS spent to execute.
        // In the unit of picoGAS, 1 picoGAS = 1e-12 GAS
        private readonly BigInteger _feeAmount;
        private BigInteger _feeConsumed;
        // Decimals for fee calculation
        public const uint FeeFactor = 10000;
        private Dictionary<Type, object>? states;
        private readonly DataCache originalSnapshotCache;
        private List<NotifyEventArgs>? notifications;
        private List<IDisposable>? disposables;
        private readonly Dictionary<UInt160, int> invocationCounter = new();
        private readonly Dictionary<ExecutionContext, ContractTaskAwaiter> contractTasks = new();
        // In the unit of picoGAS, 1 picoGAS = 1e-12 GAS
        private readonly BigInteger _execFeeFactor;
        // In the unit of datoshi, 1 datoshi = 1e-8 GAS
        internal readonly uint StoragePrice;
        private byte[] nonceData;

        /// <summary>
        /// Gets or sets the provider used to create the <see cref="ApplicationEngine"/>.
        /// </summary>
        public static IApplicationEngineProvider? Provider { get; set; }

        /// <summary>
        /// Gets the descriptors of all interoperable services available in NEO.
        /// </summary>
        public static IReadOnlyDictionary<uint, InteropDescriptor> Services => services ?? (IReadOnlyDictionary<uint, InteropDescriptor>)ImmutableDictionary<uint, InteropDescriptor>.Empty;

        /// <summary>
        /// The diagnostic used by the engine. This property can be <see langword="null"/>.
        /// </summary>
        public IDiagnostic? Diagnostic { get; }

        private List<IDisposable> Disposables => disposables ??= new List<IDisposable>();

        /// <summary>
        /// The trigger of the execution.
        /// </summary>
        public TriggerType Trigger { get; }

        /// <summary>
        /// The container that containing the executed script. This field could be <see langword="null"/> if the contract is invoked by system.
        /// </summary>
        public IVerifiable? ScriptContainer { get; }

        /// <summary>
        /// The snapshot used to read or write data.
        /// </summary>
        [Obsolete("This property is deprecated. Use SnapshotCache instead.")]
        public DataCache Snapshot => CurrentContext?.GetState<ExecutionContextState>().SnapshotCache ?? originalSnapshotCache;

        /// <summary>
        /// The snapshotcache <see cref="SnapshotCache"/> used to read or write data.
        /// </summary>
        public DataCache SnapshotCache => CurrentContext?.GetState<ExecutionContextState>().SnapshotCache ?? originalSnapshotCache;

        /// <summary>
        /// The block being persisted. This field could be <see langword="null"/> if the <see cref="Trigger"/> is <see cref="TriggerType.Verification"/>.
        /// </summary>
        public Block? PersistingBlock { get; }

        /// <summary>
        /// The <see cref="Neo.ProtocolSettings"/> used by the engine.
        /// </summary>
        public ProtocolSettings ProtocolSettings { get; }

        /// <summary>
        /// GAS spent to execute.
        /// In the unit of datoshi, 1 datoshi = 1e-8 GAS, 1 GAS = 1e8 datoshi
        /// </summary>
        [Obsolete("This property is deprecated. Use FeeConsumed instead.")]
        public long GasConsumed => FeeConsumed;

        /// <summary>
        /// Exec Fee Factor. In the unit of datoshi, 1 datoshi = 1e-8 GAS
        /// </summary>
        internal long ExecFeeFactor => (long)_execFeeFactor.DivideCeiling(FeeFactor);

        /// <summary>
        /// GAS spent to execute.
        /// In the unit of datoshi, 1 datoshi = 1e-8 GAS, 1 GAS = 1e8 datoshi
        /// </summary>
        public long FeeConsumed => (long)_feeConsumed.DivideCeiling(FeeFactor);

        /// <summary>
        /// Exec Fee Factor. In the unit of picoGAS, 1 picoGAS = 1e-12 GAS
        /// </summary>
        internal BigInteger ExecFeePicoFactor => _execFeeFactor;

        /// <summary>
        /// The remaining GAS that can be spent in order to complete the execution.
        /// In the unit of datoshi, 1 datoshi = 1e-8 GAS, 1 GAS = 1e8 datoshi
        /// </summary>
        public long GasLeft => (long)((_feeAmount - _feeConsumed) / FeeFactor);

        /// <summary>
        /// The exception that caused the execution to terminate abnormally. This field could be <see langword="null"/> if no exception is thrown.
        /// </summary>
        public Exception? FaultException { get; protected set; }

        /// <summary>
        /// The script hash of the current context. This field could be <see langword="null"/> if no context is loaded to the engine.
        /// </summary>
        public UInt160? CurrentScriptHash => CurrentContext?.GetScriptHash();

        /// <summary>
        /// The script hash of the calling contract. This field could be <see langword="null"/> if the current context is the entry context.
        /// </summary>
        public virtual UInt160? CallingScriptHash
        {
            get
            {
                if (CurrentContext is null) return null;
                var state = CurrentContext.GetState<ExecutionContextState>();
                return state.NativeCallingScriptHash ?? state.CallingContext?.GetState<ExecutionContextState>().ScriptHash;
            }
        }

        /// <summary>
        /// The script hash of the entry context. This field could be <see langword="null"/> if no context is loaded to the engine.
        /// </summary>
        public virtual UInt160? EntryScriptHash => EntryContext?.GetScriptHash();

        /// <summary>
        /// The notifications sent during the execution.
        /// </summary>
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications ?? (IReadOnlyList<NotifyEventArgs>)Array.Empty<NotifyEventArgs>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationEngine"/> class.
        /// </summary>
        /// <param name="trigger">The trigger of the execution.</param>
        /// <param name="container">The container of the script.</param>
        /// <param name="snapshotCache">The snapshot used by the engine during execution.</param>
        /// <param name="persistingBlock">
        /// The block being persisted.
        /// It should be <see langword="null"/> if the <paramref name="trigger"/> is <see cref="TriggerType.Verification"/>.
        /// </param>
        /// <param name="settings">The <see cref="Neo.ProtocolSettings"/> used by the engine.</param>
        /// <param name="gas">
        /// The maximum gas, in the unit of datoshi, used in this execution.
        /// The execution will fail when the gas is exhausted.
        /// </param>
        /// <param name="diagnostic">The diagnostic to be used by the <see cref="ApplicationEngine"/>.</param>
        /// <param name="jumpTable">The jump table to be used by the <see cref="ApplicationEngine"/>.</param>
        protected ApplicationEngine(
            TriggerType trigger, IVerifiable? container, DataCache snapshotCache, Block? persistingBlock,
            ProtocolSettings settings, long gas, IDiagnostic? diagnostic = null, JumpTable? jumpTable = null)
            : base(jumpTable ?? DefaultJumpTable)
        {
            Trigger = trigger;
            ScriptContainer = container;
            originalSnapshotCache = snapshotCache;
            PersistingBlock = persistingBlock;
            ProtocolSettings = settings;
            _feeAmount = gas * FeeFactor; // PicoGAS
            Diagnostic = diagnostic;
            nonceData = container is Transaction tx ? tx.Hash.ToArray()[..16] : new byte[16];
            if (snapshotCache is null || persistingBlock?.Index == 0)
            {
                _execFeeFactor = PolicyContract.DefaultExecFeeFactor * FeeFactor; // Add fee decimals
                StoragePrice = PolicyContract.DefaultStoragePrice;
            }
            else
            {
                var persistingIndex = persistingBlock?.Index ?? NativeContract.Ledger.CurrentIndex(snapshotCache);

                if (settings == null || !settings.IsHardforkEnabled(Hardfork.HF_Faun, persistingIndex))
                {
                    // The values doesn't have the decimals stored
                    _execFeeFactor = NativeContract.Policy.GetExecFeeFactor(this) * FeeFactor;
                }
                else
                {
                    // The values have the decimals stored starting from OnPersist of Faun's block.
                    _execFeeFactor = NativeContract.Policy.GetExecPicoFeeFactor(this);
                    if (trigger == TriggerType.OnPersist && persistingIndex > 0 && !settings.IsHardforkEnabled(Hardfork.HF_Faun, persistingIndex - 1))
                        _execFeeFactor *= FeeFactor;
                }

                StoragePrice = NativeContract.Policy.GetStoragePrice(snapshotCache);
            }

            if (persistingBlock is not null)
            {
                ref ulong nonce = ref Unsafe.As<byte, ulong>(ref nonceData[0]);
                nonce ^= persistingBlock.Nonce;
            }
            diagnostic?.Initialized(this);
        }

        #region JumpTable

        private static JumpTable ComposeDefaultJumpTable()
        {
            var table = new JumpTable();

            table[OpCode.SYSCALL] = OnSysCall;
            table[OpCode.CALLT] = OnCallT;

            return table;
        }

        public static JumpTable ComposeNotEchidnaJumpTable()
        {
            var table = ComposeDefaultJumpTable();

            table[OpCode.SUBSTR] = VulnerableSubStr;
            Patch543(table);

            return table;
        }

        public static JumpTable ComposeNotFaunJumpTable()
        {
            var table = ComposeDefaultJumpTable();
            Patch543(table);
            return table;
        }

        private static JumpTable Patch543(JumpTable table)
        {
            // Before https://github.com/neo-project/neo-vm/pull/543

            table[OpCode.HASKEY] = HasKey_Before543;
            table[OpCode.PICKITEM] = PickItem_Before543;
            table[OpCode.SETITEM] = SetItem_Before543;
            table[OpCode.REMOVE] = Remove_Before543;

            return table;
        }

        private static void Remove_Before543(ExecutionEngine engine, Instruction instruction)
        {
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    var index = (int)key.GetInteger();
                    if (index < 0 || index >= array.Count)
                        throw new InvalidOperationException($"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).");
                    array.RemoveAt(index);
                    break;
                case Map map:
                    map.Remove(key);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        private static void SetItem_Before543(ExecutionEngine engine, Instruction instruction)
        {
            var value = engine.Pop();
            if (value is Struct s) value = s.Clone(engine.Limits);
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= array.Count)
                            throw new CatchableException($"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).");
                        array[index] = value;
                        break;
                    }
                case Map map:
                    {
                        map[key] = value;
                        break;
                    }
                case VM.Types.Buffer buffer:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The index of {nameof(Buffer)} is out of range, {index}/[0, {buffer.Size}).");
                        if (value is not PrimitiveType p)
                            throw new InvalidOperationException($"Only primitive type values can be set in {nameof(Buffer)} in {instruction.OpCode}.");
                        var b = (int)p.GetInteger();
                        if (b < sbyte.MinValue || b > byte.MaxValue)
                            throw new InvalidOperationException($"Overflow in {instruction.OpCode}, {b} is not a byte type.");
                        buffer.InnerBuffer.Span[index] = (byte)b;
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        private static void PickItem_Before543(ExecutionEngine engine, Instruction instruction)
        {
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            switch (x)
            {
                case VMArray array:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= array.Count)
                            throw new CatchableException($"The index of {nameof(VMArray)} is out of range, {index}/[0, {array.Count}).");
                        engine.Push(array[index]);
                        break;
                    }
                case Map map:
                    {
                        if (!map.TryGetValue(key, out var value))
                            throw new CatchableException($"Key {key} not found in {nameof(Map)}.");
                        engine.Push(value);
                        break;
                    }
                case PrimitiveType primitive:
                    {
                        var byteArray = primitive.GetSpan();
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= byteArray.Length)
                            throw new CatchableException($"The index of {nameof(PrimitiveType)} is out of range, {index}/[0, {byteArray.Length}).");
                        engine.Push((BigInteger)byteArray[index]);
                        break;
                    }
                case Buffer buffer:
                    {
                        var index = (int)key.GetInteger();
                        if (index < 0 || index >= buffer.Size)
                            throw new CatchableException($"The index of {nameof(Buffer)} is out of range, {index}/[0, {buffer.Size}).");
                        engine.Push((BigInteger)buffer.InnerBuffer.Span[index]);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }

        private static void HasKey_Before543(ExecutionEngine engine, Instruction instruction)
        {
            var key = engine.Pop<PrimitiveType>();
            var x = engine.Pop();
            // Check the type of the top item and perform the corresponding action.
            switch (x)
            {
                // For arrays, check if the index is within bounds and push the result onto the stack.
                case VMArray array:
                    {
                        // TODO: Overflow and underflow checking needs to be done.
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative index {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < array.Count);
                        break;
                    }
                // For maps, check if the key exists and push the result onto the stack.
                case Map map:
                    {
                        engine.Push(map.ContainsKey(key));
                        break;
                    }
                // For buffers, check if the index is within bounds and push the result onto the stack.
                case VM.Types.Buffer buffer:
                    {
                        // TODO: Overflow and underflow checking needs to be done.
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative index {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < buffer.Size);
                        break;
                    }
                // For byte strings, check if the index is within bounds and push the result onto the stack.
                case ByteString array:
                    {
                        // TODO: Overflow and underflow checking needs to be done.
                        var index = (int)key.GetInteger();
                        if (index < 0)
                            throw new InvalidOperationException($"The negative index {index} is invalid for OpCode.{instruction.OpCode}.");
                        engine.Push(index < array.Size);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"Invalid type for {instruction.OpCode}: {x.Type}");
            }
        }


        protected static void OnCallT(ExecutionEngine engine, Instruction instruction)
        {
            if (engine is ApplicationEngine app)
            {
                uint tokenId = instruction.TokenU16;

                app.ValidateCallFlags(CallFlags.ReadStates | CallFlags.AllowCall);
                ContractState? contract = app.CurrentContext!.GetState<ExecutionContextState>().Contract;
                if (contract is null || tokenId >= contract.Nef.Tokens.Length)
                    throw new InvalidOperationException();
                MethodToken token = contract.Nef.Tokens[tokenId];
                if (token.ParametersCount > app.CurrentContext.EvaluationStack.Count)
                    throw new InvalidOperationException();
                StackItem[] args = new StackItem[token.ParametersCount];
                for (int i = 0; i < token.ParametersCount; i++)
                    args[i] = app.Pop();
                app.CallContractInternal(token.Hash, token.Method, token.CallFlags, token.HasReturnValue, args);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        protected static void OnSysCall(ExecutionEngine engine, Instruction instruction)
        {
            if (engine is ApplicationEngine app)
            {
                var interop = GetInteropDescriptor(instruction.TokenU32);

                if (interop.Hardfork != null && !app.IsHardforkEnabled(interop.Hardfork.Value))
                {
                    // The syscall is not active

                    throw new KeyNotFoundException();
                }

                app.OnSysCall(interop);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        /// <summary>
        /// Adds GAS to <see cref="FeeConsumed"/> and checks if it has exceeded the maximum limit.
        /// </summary>
        /// <param name="picoGas">The amount of GAS, in the unit of picoGAS, 1 picoGAS = 1e-12 GAS, to be added.</param>
        protected internal void AddFee(BigInteger picoGas)
        {
            // Check whitelist

            if (CurrentContext?.GetState<ExecutionContextState>()?.WhiteListed == true)
            {
                // The execution is whitelisted
                return;
            }

            _feeConsumed = _feeConsumed + picoGas;
            if (_feeConsumed > _feeAmount)
                throw new InvalidOperationException("Insufficient GAS.");
        }

        protected override void OnFault(Exception ex)
        {
            FaultException = ex;
            notifications = null;
            base.OnFault(ex);
        }

        internal void Throw(Exception ex)
        {
            OnFault(ex);
        }

        private ExecutionContext CallContractInternal(UInt160 contractHash, string method, CallFlags flags, bool hasReturnValue, StackItem[] args)
        {
            ContractState? contract = NativeContract.ContractManagement.GetContract(SnapshotCache, contractHash);
            if (contract is null) throw new InvalidOperationException($"Called Contract Does Not Exist: {contractHash}");
            ContractMethodDescriptor? md = contract.Manifest.Abi.GetMethod(method, args.Length);
            if (md is null) throw new InvalidOperationException($"Method \"{method}\" with {args.Length} parameter(s) doesn't exist in the contract {contractHash}.");
            return CallContractInternal(contract, md, flags, hasReturnValue, args);
        }

        private ExecutionContext CallContractInternal(ContractState contract, ContractMethodDescriptor method, CallFlags flags, bool hasReturnValue, IReadOnlyList<StackItem> args)
        {
            if (NativeContract.Policy.IsBlocked(SnapshotCache, contract.Hash))
                throw new InvalidOperationException($"The contract {contract.Hash} has been blocked.");

            ExecutionContext currentContext = CurrentContext!;
            ExecutionContextState state = currentContext.GetState<ExecutionContextState>();
            if (method.Safe)
            {
                flags &= ~(CallFlags.WriteStates | CallFlags.AllowNotify);
            }
            else
            {
                var executingContract = IsHardforkEnabled(Hardfork.HF_Domovoi)
                    ? state.Contract // use executing contract state to avoid possible contract update/destroy side-effects, ref. https://github.com/neo-project/neo/pull/3290.
                    : NativeContract.ContractManagement.GetContract(SnapshotCache, CurrentScriptHash!);
                if (executingContract?.CanCall(contract, method.Name) == false)
                    throw new InvalidOperationException($"Cannot Call Method {method.Name} Of Contract {contract.Hash} From Contract {CurrentScriptHash}");
            }

            // Check whitelist

            if (IsHardforkEnabled(Hardfork.HF_Faun) &&
                NativeContract.Policy.IsWhitelistFeeContract(SnapshotCache, contract.Hash, method, out var fixedFee))
            {
                AddFee(fixedFee.Value * ApplicationEngine.FeeFactor);
                state.WhiteListed = true;
            }

            if (invocationCounter.TryGetValue(contract.Hash, out var counter))
            {
                invocationCounter[contract.Hash] = counter + 1;
            }
            else
            {
                invocationCounter[contract.Hash] = 1;
            }

            CallFlags callingFlags = state.CallFlags;

            if (args.Count != method.Parameters.Length) throw new InvalidOperationException($"Method {method} Expects {method.Parameters.Length} Arguments But Receives {args.Count} Arguments");
            if (hasReturnValue ^ (method.ReturnType != ContractParameterType.Void)) throw new InvalidOperationException("The return value type does not match.");

            var contextNew = LoadContract(contract, method, flags & callingFlags);
            state = contextNew.GetState<ExecutionContextState>();
            state.CallingContext = currentContext;

            for (int i = args.Count - 1; i >= 0; i--)
                contextNew.EvaluationStack.Push(args[i]);

            return contextNew;
        }

        internal ContractTask CallFromNativeContractAsync(UInt160 callingScriptHash, UInt160 hash, string method, params StackItem[] args)
        {
            var contextNew = CallContractInternal(hash, method, CallFlags.All, false, args);
            var state = contextNew.GetState<ExecutionContextState>();
            state.NativeCallingScriptHash = callingScriptHash;
            ContractTask task = new();
            contractTasks.Add(contextNew, task.GetAwaiter());
            return task;
        }

        internal ContractTask<T> CallFromNativeContractAsync<T>(UInt160 callingScriptHash, UInt160 hash, string method, params StackItem[] args)
        {
            var contextNew = CallContractInternal(hash, method, CallFlags.All, true, args);
            var state = contextNew.GetState<ExecutionContextState>();
            state.NativeCallingScriptHash = callingScriptHash;
            ContractTask<T> task = new();
            contractTasks.Add(contextNew, task.GetAwaiter());
            return task;
        }

        protected override void ContextUnloaded(ExecutionContext context)
        {
            base.ContextUnloaded(context);
            if (context.Script != CurrentContext?.Script)
            {
                ExecutionContextState state = context.GetState<ExecutionContextState>();
                if (UncaughtException is null)
                {
                    state.SnapshotCache?.Commit();
                    if (CurrentContext != null)
                    {
                        ExecutionContextState contextState = CurrentContext.GetState<ExecutionContextState>();
                        contextState.NotificationCount += state.NotificationCount;
                        if (state.IsDynamicCall)
                        {
                            if (context.EvaluationStack.Count == 0)
                                Push(StackItem.Null);
                            else if (context.EvaluationStack.Count > 1)
                                throw new NotSupportedException("Multiple return values are not allowed in cross-contract calls.");
                        }
                    }
                }
                else
                {
                    if (state.NotificationCount > 0)
                        notifications!.RemoveRange(notifications.Count - state.NotificationCount, state.NotificationCount);
                }
            }
            Diagnostic?.ContextUnloaded(context);
            if (contractTasks.Remove(context, out var awaiter))
            {
                if (UncaughtException is not null)
                    throw new VMUnhandledException(UncaughtException);
                awaiter.SetResult(this);
            }
        }

        /// <summary>
        /// Use the loaded <see cref="IApplicationEngineProvider"/> to create a new instance of the <see cref="ApplicationEngine"/> class.
        /// If no <see cref="IApplicationEngineProvider"/> is loaded, the constructor of <see cref="ApplicationEngine"/> will be called.
        /// </summary>
        /// <param name="trigger">The trigger of the execution.</param>
        /// <param name="container">The container of the script.</param>
        /// <param name="snapshot">The snapshot used by the engine during execution.</param>
        /// <param name="persistingBlock">
        /// The block being persisted.
        /// It should be <see langword="null"/> if the <paramref name="trigger"/> is <see cref="TriggerType.Verification"/>.
        /// </param>
        /// <param name="settings">The <see cref="Neo.ProtocolSettings"/> used by the engine.</param>
        /// <param name="gas">
        /// The maximum gas used in this execution, in the unit of datoshi.
        /// The execution will fail when the gas is exhausted.
        /// </param>
        /// <param name="diagnostic">The diagnostic to be used by the <see cref="ApplicationEngine"/>.</param>
        /// <returns>The engine instance created.</returns>
        public static ApplicationEngine Create(TriggerType trigger, IVerifiable? container, DataCache snapshot, Block? persistingBlock = null, ProtocolSettings? settings = null, long gas = TestModeGas, IDiagnostic? diagnostic = null)
        {
            var index = persistingBlock?.Index ?? NativeContract.Ledger.CurrentIndex(snapshot);
            settings ??= ProtocolSettings.Default;
            // Adjust jump table according persistingBlock

            JumpTable jumpTable;

            if (settings.IsHardforkEnabled(Hardfork.HF_Faun, index))
            {
                jumpTable = DefaultJumpTable;
            }
            else
            {
                if (!settings.IsHardforkEnabled(Hardfork.HF_Echidna, index))
                {
                    jumpTable = NotEchidnaJumpTable;
                }
                else
                {
                    jumpTable = NotFaunJumpTable;
                }
            }

            var engine = Provider?.Create(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic, jumpTable)
                  ?? new ApplicationEngine(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic, jumpTable);

            InstanceHandler?.Invoke(engine);
            return engine;
        }

        /// <summary>
        /// Extracts a substring from the specified buffer and pushes it onto the evaluation stack.
        /// <see cref="OpCode.SUBSTR"/>
        /// </summary>
        /// <param name="engine">The execution engine.</param>
        /// <param name="instruction">The instruction being executed.</param>
        /// <remarks>Pop 3, Push 1</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void VulnerableSubStr(ExecutionEngine engine, Instruction instruction)
        {
            var count = (int)engine.Pop().GetInteger();
            if (count < 0)
                throw new InvalidOperationException($"The count can not be negative for {nameof(OpCode.SUBSTR)}, count: {count}.");
            var index = (int)engine.Pop().GetInteger();
            if (index < 0)
                throw new InvalidOperationException($"The index can not be negative for {nameof(OpCode.SUBSTR)}, index: {index}.");
            var x = engine.Pop().GetSpan();
            // Note: here it's the main change
            if (index + count > x.Length)
                throw new InvalidOperationException($"The index + count is out of range for {nameof(OpCode.SUBSTR)}, index: {index}, count: {count}, {index + count}/[0, {x.Length}].");

            Buffer result = new(count, false);
            x.Slice(index, count).CopyTo(result.InnerBuffer.Span);
            engine.Push(result);
        }

        public override void LoadContext(ExecutionContext context)
        {
            // Set default execution context state
            var state = context.GetState<ExecutionContextState>();
            state.ScriptHash ??= ((ReadOnlyMemory<byte>)context.Script).Span.ToScriptHash();
            invocationCounter.TryAdd(state.ScriptHash, 1);
            base.LoadContext(context);
            Diagnostic?.ContextLoaded(context);
        }

        /// <summary>
        /// Loads a deployed contract to the invocation stack. If the _initialize method is found on the contract, loads it as well.
        /// </summary>
        /// <param name="contract">The contract to be loaded.</param>
        /// <param name="method">The method of the contract to be called.</param>
        /// <param name="callFlags">The <see cref="CallFlags"/> used to call the method.</param>
        /// <returns>The loaded context.</returns>
        public ExecutionContext LoadContract(ContractState contract, ContractMethodDescriptor method, CallFlags callFlags)
        {
            ExecutionContext context = LoadScript(contract.Script,
                rvcount: method.ReturnType == ContractParameterType.Void ? 0 : 1,
                initialPosition: method.Offset,
                configureState: p =>
                {
                    p.CallFlags = callFlags;
                    p.ScriptHash = contract.Hash;
                    p.Contract = new ContractState
                    {
                        Id = contract.Id,
                        UpdateCounter = contract.UpdateCounter,
                        Hash = contract.Hash,
                        Nef = contract.Nef,
                        Manifest = contract.Manifest
                    };
                });

            // Call initialization
            var init = contract.Manifest.Abi.GetMethod(ContractBasicMethod.Initialize, ContractBasicMethod.InitializePCount);
            if (init is not null)
            {
                LoadContext(context.Clone(init.Offset));
            }

            return context;
        }

        /// <summary>
        /// Loads a script to the invocation stack.
        /// </summary>
        /// <param name="script">The script to be loaded.</param>
        /// <param name="rvcount">The number of return values of the script.</param>
        /// <param name="initialPosition">The initial position of the instruction pointer.</param>
        /// <param name="configureState">The action used to configure the state of the loaded context.</param>
        /// <returns>The loaded context.</returns>
        public ExecutionContext LoadScript(Script script, int rvcount = -1, int initialPosition = 0, Action<ExecutionContextState>? configureState = null)
        {
            // Create and configure context
            ExecutionContext context = CreateContext(script, rvcount, initialPosition);
            ExecutionContextState state = context.GetState<ExecutionContextState>();
            state.SnapshotCache = SnapshotCache?.CloneCache();
            configureState?.Invoke(state);

            // Load context
            LoadContext(context);
            return context;
        }

        /// <summary>
        /// Converts an <see cref="object"/> to a <see cref="StackItem"/> that used in the virtual machine.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to convert.</param>
        /// <returns>The converted <see cref="StackItem"/>.</returns>
        protected internal StackItem Convert(object? value)
        {
            if (value is IDisposable disposable) Disposables.Add(disposable);
            return value switch
            {
                null => StackItem.Null,
                bool b => b,
                sbyte i => i,
                byte i => (BigInteger)i,
                short i => i,
                ushort i => (BigInteger)i,
                int i => i,
                uint i => i,
                long i => i,
                ulong i => i,
                Enum e => Convert(System.Convert.ChangeType(e, e.GetTypeCode())),
                byte[] data => data,
                ReadOnlyMemory<byte> m => m,
                string s => s,
                BigInteger i => i,
                JObject o => o.ToByteArray(false),
                IInteroperable interoperable => interoperable.ToStackItem(ReferenceCounter),
                ISerializable i => i.ToArray(),
                StackItem item => item,
                (object a, object b) => new Struct(ReferenceCounter) { Convert(a), Convert(b) },
                Array array => new VMArray(ReferenceCounter, array.OfType<object>().Select(p => Convert(p))),
                _ => StackItem.FromInterface(value)
            };
        }

        /// <summary>
        /// Converts a <see cref="StackItem"/> to an <see cref="object"/> that to be used as an argument of an interoperable service or native contract.
        /// </summary>
        /// <param name="item">The <see cref="StackItem"/> to convert.</param>
        /// <param name="descriptor">The descriptor of the parameter.</param>
        /// <returns>The converted <see cref="object"/>.</returns>
        protected internal object? Convert(StackItem item, InteropParameterDescriptor descriptor)
        {
            if (item.IsNull && !descriptor.IsNullable && descriptor.Type != typeof(StackItem))
                throw new InvalidOperationException($"The argument `{descriptor.Name}` can't be null.");
            descriptor.Validate(item);
            if (descriptor.IsArray)
            {
                Array av;
                if (item is VMArray array)
                {
                    av = Array.CreateInstance(descriptor.Type.GetElementType()!, array.Count);
                    for (int i = 0; i < av.Length; i++)
                    {
                        if (array[i].IsNull && !descriptor.IsElementNullable)
                            throw new InvalidOperationException($"The element of `{descriptor.Name}` can't be null.");
                        av.SetValue(descriptor.Converter(array[i]), i);
                    }
                }
                else
                {
                    int count = (int)item.GetInteger();
                    if (count > Limits.MaxStackSize) throw new InvalidOperationException();
                    av = Array.CreateInstance(descriptor.Type.GetElementType()!, count);
                    for (int i = 0; i < av.Length; i++)
                    {
                        StackItem popped = Pop();
                        if (popped.IsNull && !descriptor.IsElementNullable)
                            throw new InvalidOperationException($"The element of `{descriptor.Name}` can't be null.");
                        av.SetValue(descriptor.Converter(popped), i);
                    }
                }
                return av;
            }
            else
            {
                object? value = descriptor.Converter(item);
                if (descriptor.IsEnum)
                    value = Enum.ToObject(descriptor.Type, value!);
                else if (descriptor.IsInterface)
                    value = ((InteropInterface?)value)?.GetInterface<object>();
                return value;
            }
        }

        public override void Dispose()
        {
            Diagnostic?.Disposed();
            if (disposables != null)
            {
                foreach (var disposable in disposables)
                    disposable.Dispose();
                disposables = null;
            }
            base.Dispose();
        }

        /// <summary>
        /// Determines whether the <see cref="CallFlags"/> of the current context meets the specified requirements.
        /// </summary>
        /// <param name="requiredCallFlags">The requirements to check.</param>
        internal protected void ValidateCallFlags(CallFlags requiredCallFlags)
        {
            ExecutionContextState state = CurrentContext!.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(requiredCallFlags))
                throw new InvalidOperationException($"Cannot call this SYSCALL with the flag {state.CallFlags}.");
        }

        /// <summary>
        /// Invokes the specified interoperable service.
        /// </summary>
        /// <param name="descriptor">The descriptor of the interoperable service.</param>
        protected virtual void OnSysCall(InteropDescriptor descriptor)
        {
            ValidateCallFlags(descriptor.RequiredCallFlags);
            AddFee(descriptor.FixedPrice * _execFeeFactor);

            object?[] parameters = new object?[descriptor.Parameters.Count];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Convert(Pop(), descriptor.Parameters[i]);

            object? returnValue = descriptor.Handler.Invoke(this, parameters);
            if (descriptor.Handler.ReturnType != typeof(void))
                Push(Convert(returnValue));
        }

        protected override void PreExecuteInstruction(Instruction instruction)
        {
            Diagnostic?.PreExecuteInstruction(instruction);
            AddFee(_execFeeFactor * OpCodePriceTable[(byte)instruction.OpCode]);
        }

        protected override void PostExecuteInstruction(Instruction instruction)
        {
            base.PostExecuteInstruction(instruction);
            Diagnostic?.PostExecuteInstruction(instruction);
        }

        private static Block CreateDummyBlock(IReadOnlyStore snapshot, ProtocolSettings settings)
        {
            UInt256 hash = NativeContract.Ledger.CurrentHash(snapshot);
            Block currentBlock = NativeContract.Ledger.GetBlock(snapshot, hash)!;
            return new Block
            {
                Header = new Header
                {
                    Version = 0,
                    PrevHash = hash,
                    MerkleRoot = new UInt256(),
                    Timestamp = currentBlock.Timestamp + (uint)snapshot.GetTimePerBlock(settings).TotalMilliseconds,
                    Index = currentBlock.Index + 1,
                    NextConsensus = currentBlock.NextConsensus,
                    Witness = Witness.Empty,
                },
                Transactions = [],
            };
        }

        protected static InteropDescriptor Register(string name, string handler, long fixedPrice, CallFlags requiredCallFlags, Hardfork? hardfork = null)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var method = typeof(ApplicationEngine).GetMethod(handler, flags)
                ?? typeof(ApplicationEngine).GetProperty(handler, flags)?.GetMethod
                ?? throw new ArgumentException($"Handler {handler} is not found.", nameof(handler));
            var descriptor = new InteropDescriptor()
            {
                Name = name,
                Handler = method,
                Hardfork = hardfork,
                FixedPrice = fixedPrice,
                RequiredCallFlags = requiredCallFlags
            };
            services ??= [];
            services.Add(descriptor.Hash, descriptor);
            return descriptor;
        }

        /// <summary>
        /// Get Interop Descriptor
        /// </summary>
        /// <param name="methodHash">Method Hash</param>
        /// <returns>InteropDescriptor</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InteropDescriptor GetInteropDescriptor(uint methodHash)
        {
            return services![methodHash];
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ApplicationEngine"/> class, and use it to run the specified script.
        /// </summary>
        /// <param name="script">The script to be executed.</param>
        /// <param name="snapshot">The snapshot used by the engine during execution.</param>
        /// <param name="container">The container of the script.</param>
        /// <param name="persistingBlock">The block being persisted.</param>
        /// <param name="settings">The <see cref="Neo.ProtocolSettings"/> used by the engine.</param>
        /// <param name="offset">The initial position of the instruction pointer.</param>
        /// <param name="gas">The maximum gas, in the unit of datoshi, used in this execution. The execution will fail when the gas is exhausted.</param>
        /// <param name="diagnostic">The diagnostic to be used by the <see cref="ApplicationEngine"/>.</param>
        /// <returns>The engine instance created.</returns>
        public static ApplicationEngine Run(ReadOnlyMemory<byte> script, DataCache snapshot, IVerifiable? container = null, Block? persistingBlock = null, ProtocolSettings? settings = null, int offset = 0, long gas = TestModeGas, IDiagnostic? diagnostic = null)
        {
            persistingBlock ??= CreateDummyBlock(snapshot, settings ?? ProtocolSettings.Default);
            ApplicationEngine engine = Create(TriggerType.Application, container, snapshot, persistingBlock, settings, gas, diagnostic);
            engine.LoadScript(script, initialPosition: offset);
            engine.Execute();
            return engine;
        }

        public T? GetState<T>() where T : notnull
        {
            if (states is null) return default;
            if (!states.TryGetValue(typeof(T), out object? state)) return default;
            return (T)state;
        }

        public T GetState<T>(Func<T> factory) where T : notnull
        {
            if (states is null)
            {
                T state = factory();
                SetState(state);
                return state;
            }
            else
            {
                if (!states.TryGetValue(typeof(T), out object? state))
                {
                    state = factory();
                    SetState(state);
                }
                return (T)state;
            }
        }

        public void SetState<T>(T state) where T : notnull
        {
            states ??= new Dictionary<Type, object>();
            states[typeof(T)] = state;
        }

        public bool IsHardforkEnabled(Hardfork hardfork)
        {
            if (ProtocolSettings == null)
                return false;

            // Return true if PersistingBlock is null and Hardfork is enabled
            if (PersistingBlock is null)
                return ProtocolSettings.Hardforks.ContainsKey(hardfork);

            return ProtocolSettings.IsHardforkEnabled(hardfork, PersistingBlock.Index);
        }
    }
}
