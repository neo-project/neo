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
using System.Linq;
using System.Numerics;
using System.Reflection;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Array = System.Array;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    /// <summary>
    /// A virtual machine used to execute smart contracts in the NEO system.
    /// </summary>
    public partial class ApplicationEngine : ExecutionEngine
    {
        /// <summary>
        /// The maximum cost that can be spent when a contract is executed in test mode.
        /// </summary>
        public const long TestModeGas = 20_00000000;

        /// <summary>
        /// Triggered when a contract calls System.Runtime.Notify.
        /// </summary>
        public static event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Triggered when a contract calls System.Runtime.Log.
        /// </summary>
        public static event EventHandler<LogEventArgs> Log;

        private static readonly IList<Hardfork> AllHardforks = Enum.GetValues(typeof(Hardfork)).Cast<Hardfork>().ToArray();
        private static Dictionary<uint, InteropDescriptor> services;
        private readonly long gas_amount;
        private Dictionary<Type, object> states;
        private readonly DataCache originalSnapshot;
        private List<NotifyEventArgs> notifications;
        private List<IDisposable> disposables;
        private readonly Dictionary<UInt160, int> invocationCounter = new();
        private readonly Dictionary<ExecutionContext, ContractTaskAwaiter> contractTasks = new();
        internal readonly uint ExecFeeFactor;
        internal readonly uint StoragePrice;
        private byte[] nonceData;

        /// <summary>
        /// Gets or sets the provider used to create the <see cref="ApplicationEngine"/>.
        /// </summary>
        public static IApplicationEngineProvider Provider { get; set; }

        /// <summary>
        /// Gets the descriptors of all interoperable services available in NEO.
        /// </summary>
        public static IReadOnlyDictionary<uint, InteropDescriptor> Services => services;

        /// <summary>
        /// The diagnostic used by the engine. This property can be <see langword="null"/>.
        /// </summary>
        public IDiagnostic Diagnostic { get; }

        private List<IDisposable> Disposables => disposables ??= new List<IDisposable>();

        /// <summary>
        /// The trigger of the execution.
        /// </summary>
        public TriggerType Trigger { get; }

        /// <summary>
        /// The container that containing the executed script. This field could be <see langword="null"/> if the contract is invoked by system.
        /// </summary>
        public IVerifiable ScriptContainer { get; }

        /// <summary>
        /// The snapshot used to read or write data.
        /// </summary>
        public DataCache Snapshot => CurrentContext?.GetState<ExecutionContextState>().Snapshot ?? originalSnapshot;

        /// <summary>
        /// The block being persisted. This field could be <see langword="null"/> if the <see cref="Trigger"/> is <see cref="TriggerType.Verification"/>.
        /// </summary>
        public Block PersistingBlock { get; }

        /// <summary>
        /// The <see cref="Neo.ProtocolSettings"/> used by the engine.
        /// </summary>
        public ProtocolSettings ProtocolSettings { get; }

        /// <summary>
        /// GAS spent to execute.
        /// </summary>
        public long GasConsumed { get; private set; } = 0;

        /// <summary>
        /// The remaining GAS that can be spent in order to complete the execution.
        /// </summary>
        public long GasLeft => gas_amount - GasConsumed;

        /// <summary>
        /// The exception that caused the execution to terminate abnormally. This field could be <see langword="null"/> if no exception is thrown.
        /// </summary>
        public Exception FaultException { get; private set; }

        /// <summary>
        /// The script hash of the current context. This field could be <see langword="null"/> if no context is loaded to the engine.
        /// </summary>
        public UInt160 CurrentScriptHash => CurrentContext?.GetScriptHash();

        /// <summary>
        /// The script hash of the calling contract. This field could be <see langword="null"/> if the current context is the entry context.
        /// </summary>
        public UInt160 CallingScriptHash
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
        public UInt160 EntryScriptHash => EntryContext?.GetScriptHash();

        /// <summary>
        /// The notifications sent during the execution.
        /// </summary>
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications ?? (IReadOnlyList<NotifyEventArgs>)Array.Empty<NotifyEventArgs>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationEngine"/> class.
        /// </summary>
        /// <param name="trigger">The trigger of the execution.</param>
        /// <param name="container">The container of the script.</param>
        /// <param name="snapshot">The snapshot used by the engine during execution.</param>
        /// <param name="persistingBlock">The block being persisted. It should be <see langword="null"/> if the <paramref name="trigger"/> is <see cref="TriggerType.Verification"/>.</param>
        /// <param name="settings">The <see cref="Neo.ProtocolSettings"/> used by the engine.</param>
        /// <param name="gas">The maximum gas used in this execution. The execution will fail when the gas is exhausted.</param>
        /// <param name="diagnostic">The diagnostic to be used by the <see cref="ApplicationEngine"/>.</param>
        protected unsafe ApplicationEngine(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, ProtocolSettings settings, long gas, IDiagnostic diagnostic)
        {
            this.Trigger = trigger;
            this.ScriptContainer = container;
            this.originalSnapshot = snapshot;
            this.PersistingBlock = persistingBlock;
            this.ProtocolSettings = settings;
            this.gas_amount = gas;
            this.Diagnostic = diagnostic;
            this.ExecFeeFactor = snapshot is null || persistingBlock?.Index == 0 ? PolicyContract.DefaultExecFeeFactor : NativeContract.Policy.GetExecFeeFactor(snapshot);
            this.StoragePrice = snapshot is null || persistingBlock?.Index == 0 ? PolicyContract.DefaultStoragePrice : NativeContract.Policy.GetStoragePrice(snapshot);
            this.nonceData = container is Transaction tx ? tx.Hash.ToArray()[..16] : new byte[16];
            if (persistingBlock is not null)
            {
                fixed (byte* p = nonceData)
                {
                    *(ulong*)p ^= persistingBlock.Nonce;
                }
            }
            diagnostic?.Initialized(this);
        }

        /// <summary>
        /// Adds GAS to <see cref="GasConsumed"/> and checks if it has exceeded the maximum limit.
        /// </summary>
        /// <param name="gas">The amount of GAS to be added.</param>
        protected internal void AddGas(long gas)
        {
            GasConsumed = checked(GasConsumed + gas);
            if (GasConsumed > gas_amount)
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
            ContractState contract = NativeContract.ContractManagement.GetContract(Snapshot, contractHash);
            if (contract is null) throw new InvalidOperationException($"Called Contract Does Not Exist: {contractHash}");
            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(method, args.Length);
            if (md is null) throw new InvalidOperationException($"Method \"{method}\" with {args.Length} parameter(s) doesn't exist in the contract {contractHash}.");
            return CallContractInternal(contract, md, flags, hasReturnValue, args);
        }

        private ExecutionContext CallContractInternal(ContractState contract, ContractMethodDescriptor method, CallFlags flags, bool hasReturnValue, IReadOnlyList<StackItem> args)
        {
            if (NativeContract.Policy.IsBlocked(Snapshot, contract.Hash))
                throw new InvalidOperationException($"The contract {contract.Hash} has been blocked.");

            if (method.Safe)
            {
                flags &= ~(CallFlags.WriteStates | CallFlags.AllowNotify);
            }
            else
            {
                ContractState currentContract = NativeContract.ContractManagement.GetContract(Snapshot, CurrentScriptHash);
                if (currentContract?.CanCall(contract, method.Name) == false)
                    throw new InvalidOperationException($"Cannot Call Method {method.Name} Of Contract {contract.Hash} From Contract {CurrentScriptHash}");
            }

            if (invocationCounter.TryGetValue(contract.Hash, out var counter))
            {
                invocationCounter[contract.Hash] = counter + 1;
            }
            else
            {
                invocationCounter[contract.Hash] = 1;
            }

            ExecutionContext currentContext = CurrentContext;
            ExecutionContextState state = currentContext.GetState<ExecutionContextState>();
            CallFlags callingFlags = state.CallFlags;

            if (args.Count != method.Parameters.Length) throw new InvalidOperationException($"Method {method} Expects {method.Parameters.Length} Arguments But Receives {args.Count} Arguments");
            if (hasReturnValue ^ (method.ReturnType != ContractParameterType.Void)) throw new InvalidOperationException("The return value type does not match.");
            ExecutionContext context_new = LoadContract(contract, method, flags & callingFlags);
            state = context_new.GetState<ExecutionContextState>();
            state.CallingContext = currentContext;

            for (int i = args.Count - 1; i >= 0; i--)
                context_new.EvaluationStack.Push(args[i]);

            return context_new;
        }

        internal ContractTask CallFromNativeContract(UInt160 callingScriptHash, UInt160 hash, string method, params StackItem[] args)
        {
            ExecutionContext context_new = CallContractInternal(hash, method, CallFlags.All, false, args);
            ExecutionContextState state = context_new.GetState<ExecutionContextState>();
            state.NativeCallingScriptHash = callingScriptHash;
            ContractTask task = new();
            contractTasks.Add(context_new, task.GetAwaiter());
            return task;
        }

        internal ContractTask<T> CallFromNativeContract<T>(UInt160 callingScriptHash, UInt160 hash, string method, params StackItem[] args)
        {
            ExecutionContext context_new = CallContractInternal(hash, method, CallFlags.All, true, args);
            ExecutionContextState state = context_new.GetState<ExecutionContextState>();
            state.NativeCallingScriptHash = callingScriptHash;
            ContractTask<T> task = new();
            contractTasks.Add(context_new, task.GetAwaiter());
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
                    state.Snapshot?.Commit();
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
                        notifications.RemoveRange(notifications.Count - state.NotificationCount, state.NotificationCount);
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
        /// Use the loaded <see cref="IApplicationEngineProvider"/> to create a new instance of the <see cref="ApplicationEngine"/> class. If no <see cref="IApplicationEngineProvider"/> is loaded, the constructor of <see cref="ApplicationEngine"/> will be called.
        /// </summary>
        /// <param name="trigger">The trigger of the execution.</param>
        /// <param name="container">The container of the script.</param>
        /// <param name="snapshot">The snapshot used by the engine during execution.</param>
        /// <param name="persistingBlock">The block being persisted. It should be <see langword="null"/> if the <paramref name="trigger"/> is <see cref="TriggerType.Verification"/>.</param>
        /// <param name="settings">The <see cref="Neo.ProtocolSettings"/> used by the engine.</param>
        /// <param name="gas">The maximum gas used in this execution. The execution will fail when the gas is exhausted.</param>
        /// <param name="diagnostic">The diagnostic to be used by the <see cref="ApplicationEngine"/>.</param>
        /// <returns>The engine instance created.</returns>
        public static ApplicationEngine Create(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock = null, ProtocolSettings settings = null, long gas = TestModeGas, IDiagnostic diagnostic = null)
        {
            return Provider?.Create(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic)
                  ?? new ApplicationEngine(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic);
        }

        protected override void LoadContext(ExecutionContext context)
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
            var init = contract.Manifest.Abi.GetMethod("_initialize", 0);
            if (init != null)
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
        public ExecutionContext LoadScript(Script script, int rvcount = -1, int initialPosition = 0, Action<ExecutionContextState> configureState = null)
        {
            // Create and configure context
            ExecutionContext context = CreateContext(script, rvcount, initialPosition);
            ExecutionContextState state = context.GetState<ExecutionContextState>();
            state.Snapshot = Snapshot?.CreateSnapshot();
            configureState?.Invoke(state);

            // Load context
            LoadContext(context);
            return context;
        }

        protected override ExecutionContext LoadToken(ushort tokenId)
        {
            ValidateCallFlags(CallFlags.ReadStates | CallFlags.AllowCall);
            ContractState contract = CurrentContext.GetState<ExecutionContextState>().Contract;
            if (contract is null || tokenId >= contract.Nef.Tokens.Length)
                throw new InvalidOperationException();
            MethodToken token = contract.Nef.Tokens[tokenId];
            if (token.ParametersCount > CurrentContext.EvaluationStack.Count)
                throw new InvalidOperationException();
            StackItem[] args = new StackItem[token.ParametersCount];
            for (int i = 0; i < token.ParametersCount; i++)
                args[i] = Pop();
            return CallContractInternal(token.Hash, token.Method, token.CallFlags, token.HasReturnValue, args);
        }

        /// <summary>
        /// Converts an <see cref="object"/> to a <see cref="StackItem"/> that used in the virtual machine.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to convert.</param>
        /// <returns>The converted <see cref="StackItem"/>.</returns>
        protected internal StackItem Convert(object value)
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
        protected internal object Convert(StackItem item, InteropParameterDescriptor descriptor)
        {
            descriptor.Validate(item);
            if (descriptor.IsArray)
            {
                Array av;
                if (item is VMArray array)
                {
                    av = Array.CreateInstance(descriptor.Type.GetElementType(), array.Count);
                    for (int i = 0; i < av.Length; i++)
                        av.SetValue(descriptor.Converter(array[i]), i);
                }
                else
                {
                    int count = (int)item.GetInteger();
                    if (count > Limits.MaxStackSize) throw new InvalidOperationException();
                    av = Array.CreateInstance(descriptor.Type.GetElementType(), count);
                    for (int i = 0; i < av.Length; i++)
                        av.SetValue(descriptor.Converter(Pop()), i);
                }
                return av;
            }
            else
            {
                object value = descriptor.Converter(item);
                if (descriptor.IsEnum)
                    value = Enum.ToObject(descriptor.Type, value);
                else if (descriptor.IsInterface)
                    value = ((InteropInterface)value).GetInterface<object>();
                return value;
            }
        }

        public override void Dispose()
        {
            Diagnostic?.Disposed();
            if (disposables != null)
            {
                foreach (IDisposable disposable in disposables)
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
            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(requiredCallFlags))
                throw new InvalidOperationException($"Cannot call this SYSCALL with the flag {state.CallFlags}.");
        }

        protected override void OnSysCall(uint method)
        {
            OnSysCall(services[method]);
        }

        /// <summary>
        /// Invokes the specified interoperable service.
        /// </summary>
        /// <param name="descriptor">The descriptor of the interoperable service.</param>
        protected virtual void OnSysCall(InteropDescriptor descriptor)
        {
            ValidateCallFlags(descriptor.RequiredCallFlags);
            AddGas(descriptor.FixedPrice * ExecFeeFactor);

            object[] parameters = new object[descriptor.Parameters.Count];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Convert(Pop(), descriptor.Parameters[i]);

            object returnValue = descriptor.Handler.Invoke(this, parameters);
            if (descriptor.Handler.ReturnType != typeof(void))
                Push(Convert(returnValue));
        }

        protected override void PreExecuteInstruction(Instruction instruction)
        {
            Diagnostic?.PreExecuteInstruction(instruction);
            AddGas(ExecFeeFactor * OpCodePrices[instruction.OpCode]);
        }

        protected override void PostExecuteInstruction(Instruction instruction)
        {
            base.PostExecuteInstruction(instruction);
            Diagnostic?.PostExecuteInstruction(instruction);
        }

        private static Block CreateDummyBlock(DataCache snapshot, ProtocolSettings settings)
        {
            UInt256 hash = NativeContract.Ledger.CurrentHash(snapshot);
            Block currentBlock = NativeContract.Ledger.GetBlock(snapshot, hash);
            return new Block
            {
                Header = new Header
                {
                    Version = 0,
                    PrevHash = hash,
                    MerkleRoot = new UInt256(),
                    Timestamp = currentBlock.Timestamp + settings.MillisecondsPerBlock,
                    Index = currentBlock.Index + 1,
                    NextConsensus = currentBlock.NextConsensus,
                    Witness = new Witness
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = Array.Empty<byte>()
                    },
                },
                Transactions = Array.Empty<Transaction>()
            };
        }

        private static InteropDescriptor Register(string name, string handler, long fixedPrice, CallFlags requiredCallFlags)
        {
            MethodInfo method = typeof(ApplicationEngine).GetMethod(handler, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                ?? typeof(ApplicationEngine).GetProperty(handler, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetMethod;
            InteropDescriptor descriptor = new()
            {
                Name = name,
                Handler = method,
                FixedPrice = fixedPrice,
                RequiredCallFlags = requiredCallFlags
            };
            services ??= new Dictionary<uint, InteropDescriptor>();
            services.Add(descriptor.Hash, descriptor);
            return descriptor;
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
        /// <param name="gas">The maximum gas used in this execution. The execution will fail when the gas is exhausted.</param>
        /// <param name="diagnostic">The diagnostic to be used by the <see cref="ApplicationEngine"/>.</param>
        /// <returns>The engine instance created.</returns>
        public static ApplicationEngine Run(ReadOnlyMemory<byte> script, DataCache snapshot, IVerifiable container = null, Block persistingBlock = null, ProtocolSettings settings = null, int offset = 0, long gas = TestModeGas, IDiagnostic diagnostic = null)
        {
            persistingBlock ??= CreateDummyBlock(snapshot, settings ?? ProtocolSettings.Default);
            ApplicationEngine engine = Create(TriggerType.Application, container, snapshot, persistingBlock, settings, gas, diagnostic);
            engine.LoadScript(script, initialPosition: offset);
            engine.Execute();
            return engine;
        }

        public T GetState<T>()
        {
            if (states is null) return default;
            if (!states.TryGetValue(typeof(T), out object state)) return default;
            return (T)state;
        }

        public void SetState<T>(T state)
        {
            states ??= new Dictionary<Type, object>();
            states[typeof(T)] = state;
        }

        public bool IsHardforkEnabled(Hardfork hardfork)
        {
            // Return true if there's no specific configuration or PersistingBlock is null
            if (PersistingBlock is null || ProtocolSettings.Hardforks.Count == 0)
                return true;

            // If the hardfork isn't specified in the configuration, check if it's a new one.
            if (!ProtocolSettings.Hardforks.ContainsKey(hardfork))
            {
                int currentHardforkIndex = AllHardforks.IndexOf(hardfork);
                int lastConfiguredHardforkIndex = AllHardforks.IndexOf(ProtocolSettings.Hardforks.Keys.Last());

                // If it's a newer hardfork compared to the ones in the configuration, disable it.
                if (currentHardforkIndex > lastConfiguredHardforkIndex)
                    return false;
            }

            if (ProtocolSettings.Hardforks.TryGetValue(hardfork, out uint height))
            {
                // If the hardfork has a specific height in the configuration, check the block height.
                return PersistingBlock.Index >= height;
            }
            // If no specific conditions are met, return true.
            return true;
        }
    }
}
