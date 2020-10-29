using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using static System.Threading.Interlocked;
using Array = System.Array;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    public partial class ApplicationEngine : ExecutionEngine
    {
        protected internal enum CheckReturnType : byte
        {
            None = 0,
            EnsureIsEmpty = 1,
            EnsureNotEmpty = 2,
            DropResult = 3
        }

        private class InvocationState
        {
            public Type ReturnType;
            public Delegate Callback;
            public CheckReturnType NeedCheckReturnValue;
        }

        /// <summary>
        /// This constant can be used for testing scripts.
        /// </summary>
        private const long TestModeGas = 20_00000000;

        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        private static IApplicationEngineProvider applicationEngineProvider;
        private static Dictionary<uint, InteropDescriptor> services;
        private readonly long gas_amount;
        private List<NotifyEventArgs> notifications;
        private List<IDisposable> disposables;
        private readonly Dictionary<UInt160, int> invocationCounter = new Dictionary<UInt160, int>();
        private readonly Dictionary<ExecutionContext, InvocationState> invocationStates = new Dictionary<ExecutionContext, InvocationState>();

        public static IReadOnlyDictionary<uint, InteropDescriptor> Services => services;
        private List<IDisposable> Disposables => disposables ??= new List<IDisposable>();
        public TriggerType Trigger { get; }
        public IVerifiable ScriptContainer { get; }
        public StoreView Snapshot { get; }
        public long GasConsumed { get; private set; } = 0;
        public long GasLeft => gas_amount - GasConsumed;
        public Exception FaultException { get; private set; }
        public UInt160 CurrentScriptHash => CurrentContext?.GetScriptHash();
        public UInt160 CallingScriptHash => CurrentContext?.GetState<ExecutionContextState>().CallingScriptHash;
        public UInt160 EntryScriptHash => EntryContext?.GetScriptHash();
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications ?? (IReadOnlyList<NotifyEventArgs>)Array.Empty<NotifyEventArgs>();

        protected ApplicationEngine(TriggerType trigger, IVerifiable container, StoreView snapshot, long gas)
        {
            this.Trigger = trigger;
            this.ScriptContainer = container;
            this.Snapshot = snapshot;
            this.gas_amount = gas;
        }

        protected internal void AddGas(long gas)
        {
            GasConsumed = checked(GasConsumed + gas);
            if (GasConsumed > gas_amount)
                throw new InvalidOperationException("Insufficient GAS.");
        }

        protected override void OnFault(Exception e)
        {
            FaultException = e;
            base.OnFault(e);
        }

        internal void CallFromNativeContract(Action onComplete, UInt160 hash, string method, params StackItem[] args)
        {
            InvocationState state = GetInvocationState(CurrentContext);
            state.ReturnType = typeof(void);
            state.Callback = onComplete;
            CallContract(hash, method, new VMArray(ReferenceCounter, args));
        }

        internal void CallFromNativeContract<T>(Action<T> onComplete, UInt160 hash, string method, params StackItem[] args)
        {
            InvocationState state = GetInvocationState(CurrentContext);
            state.ReturnType = typeof(T);
            state.Callback = onComplete;
            CallContract(hash, method, new VMArray(ReferenceCounter, args));
        }

        protected override void ContextUnloaded(ExecutionContext context)
        {
            base.ContextUnloaded(context);
            if (!(UncaughtException is null)) return;
            if (invocationStates.Count == 0) return;
            if (!invocationStates.Remove(CurrentContext, out InvocationState state)) return;
            switch (state.NeedCheckReturnValue)
            {
                case CheckReturnType.EnsureIsEmpty:
                    {
                        if (context.EvaluationStack.Count != 0)
                            throw new InvalidOperationException();
                        break;
                    }
                case CheckReturnType.EnsureNotEmpty:
                    {
                        if (context.EvaluationStack.Count == 0)
                            Push(StackItem.Null);
                        else if (context.EvaluationStack.Count > 1)
                            throw new InvalidOperationException();
                        break;
                    }
                case CheckReturnType.DropResult:
                    {
                        if (context.EvaluationStack.Count == 1)
                            context.EvaluationStack.Pop();
                        else throw new InvalidOperationException();
                        break;
                    }
            }
            switch (state.Callback)
            {
                case null:
                    break;
                case Action action:
                    action();
                    break;
                default:
                    state.Callback.DynamicInvoke(Convert(Pop(), new InteropParameterDescriptor(state.ReturnType)));
                    break;
            }
        }

        public static ApplicationEngine Create(TriggerType trigger, IVerifiable container, StoreView snapshot, long gas = TestModeGas)
        {
            return applicationEngineProvider?.Create(trigger, container, snapshot, gas)
                  ?? new ApplicationEngine(trigger, container, snapshot, gas);
        }

        private InvocationState GetInvocationState(ExecutionContext context)
        {
            if (!invocationStates.TryGetValue(context, out InvocationState state))
            {
                state = new InvocationState();
                invocationStates.Add(context, state);
            }
            return state;
        }

        protected override void LoadContext(ExecutionContext context)
        {
            // Set default execution context state

            var state = context.GetState<ExecutionContextState>();
            state.ScriptHash ??= ((byte[])context.Script).ToScriptHash();
            invocationCounter.TryAdd(state.ScriptHash, 1);

            base.LoadContext(context);
        }

        internal void LoadContext(ExecutionContext context, bool checkReturnValue)
        {
            if (checkReturnValue)
                GetInvocationState(CurrentContext).NeedCheckReturnValue = CheckReturnType.EnsureNotEmpty;
            LoadContext(context);
        }

        public ExecutionContext LoadScript(Script script, CallFlags callFlags, int initialPosition = 0)
        {
            ExecutionContext context = LoadScript(script, initialPosition);
            context.GetState<ExecutionContextState>().CallFlags = callFlags;
            return context;
        }

        protected internal StackItem Convert(object value)
        {
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
                string s => s,
                BigInteger i => i,
                IInteroperable interoperable => interoperable.ToStackItem(ReferenceCounter),
                ISerializable i => i.ToArray(),
                StackItem item => item,
                (object a, object b) => new Struct(ReferenceCounter) { Convert(a), Convert(b) },
                Array array => new VMArray(ReferenceCounter, array.OfType<object>().Select(p => Convert(p))),
                _ => StackItem.FromInterface(value)
            };
        }

        protected internal object Convert(StackItem item, InteropParameterDescriptor descriptor)
        {
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
            if (disposables != null)
            {
                foreach (IDisposable disposable in disposables)
                    disposable.Dispose();
                disposables = null;
            }
            base.Dispose();
        }

        protected void ValidateCallFlags(InteropDescriptor descriptor)
        {
            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(descriptor.RequiredCallFlags))
                throw new InvalidOperationException($"Cannot call this SYSCALL with the flag {state.CallFlags}.");
        }

        protected override void OnSysCall(uint method)
        {
            InteropDescriptor descriptor = services[method];
            ValidateCallFlags(descriptor);
            AddGas(descriptor.FixedPrice);
            List<object> parameters = descriptor.Parameters.Count > 0
                ? new List<object>()
                : null;
            foreach (var pd in descriptor.Parameters)
                parameters.Add(Convert(Pop(), pd));
            object returnValue = descriptor.Handler.Invoke(this, parameters?.ToArray());
            if (descriptor.Handler.ReturnType != typeof(void))
                Push(Convert(returnValue));
        }

        protected override void PreExecuteInstruction()
        {
            if (CurrentContext.InstructionPointer < CurrentContext.Script.Length)
                AddGas(OpCodePrices[CurrentContext.CurrentInstruction.OpCode]);
        }

        private static Block CreateDummyBlock(StoreView snapshot)
        {
            var currentBlock = snapshot.Blocks[snapshot.CurrentBlockHash];
            return new Block
            {
                Version = 0,
                PrevHash = snapshot.CurrentBlockHash,
                MerkleRoot = new UInt256(),
                Timestamp = currentBlock.Timestamp + Blockchain.MillisecondsPerBlock,
                Index = snapshot.Height + 1,
                NextConsensus = currentBlock.NextConsensus,
                Witness = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                },
                ConsensusData = new ConsensusData(),
                Transactions = new Transaction[0]
            };
        }

        private static InteropDescriptor Register(string name, string handler, long fixedPrice, CallFlags requiredCallFlags, bool allowCallback)
        {
            MethodInfo method = typeof(ApplicationEngine).GetMethod(handler, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? typeof(ApplicationEngine).GetProperty(handler, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetMethod;
            InteropDescriptor descriptor = new InteropDescriptor(name, method, fixedPrice, requiredCallFlags, allowCallback);
            services ??= new Dictionary<uint, InteropDescriptor>();
            services.Add(descriptor.Hash, descriptor);
            return descriptor;
        }

        internal static void ResetApplicationEngineProvider()
        {
            Exchange(ref applicationEngineProvider, null);
        }

        public static ApplicationEngine Run(byte[] script, StoreView snapshot = null, IVerifiable container = null, Block persistingBlock = null, int offset = 0, long gas = TestModeGas)
        {
            SnapshotView disposable = null;
            if (snapshot is null)
            {
                disposable = Blockchain.Singleton.GetSnapshot();
                snapshot = disposable;
            }
            snapshot.PersistingBlock = persistingBlock ?? snapshot.PersistingBlock ?? CreateDummyBlock(snapshot);
            ApplicationEngine engine = Create(TriggerType.Application, container, snapshot, gas);
            if (disposable != null) engine.Disposables.Add(disposable);
            engine.LoadScript(script, offset);
            engine.Execute();
            return engine;
        }

        internal static bool SetApplicationEngineProvider(IApplicationEngineProvider provider)
        {
            return CompareExchange(ref applicationEngineProvider, provider, null) is null;
        }
    }
}
