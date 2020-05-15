using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Array = System.Array;

namespace Neo.SmartContract
{
    public partial class ApplicationEngine : ExecutionEngine
    {
        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        public const long GasFree = 0;
        private readonly long gas_amount;
        private readonly bool testMode;
        private readonly List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();
        private readonly Dictionary<UInt160, int> invocationCounter = new Dictionary<UInt160, int>();
        private static readonly Dictionary<uint, InteropDescriptor> services = new Dictionary<uint, InteropDescriptor>();

        public TriggerType Trigger { get; }
        public IVerifiable ScriptContainer { get; }
        public StoreView Snapshot { get; }
        public long GasConsumed { get; private set; } = 0;
        public long GasLeft => testMode ? -1 : gas_amount - GasConsumed;
        public static IEnumerable<InteropDescriptor> Services => services.Values;

        public UInt160 CurrentScriptHash => CurrentContext?.GetState<ExecutionContextState>().ScriptHash;
        public UInt160 CallingScriptHash => CurrentContext?.GetState<ExecutionContextState>().CallingScriptHash;
        public UInt160 EntryScriptHash => EntryContext?.GetState<ExecutionContextState>().ScriptHash;
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;

        static ApplicationEngine()
        {
            foreach (MethodInfo method in typeof(ApplicationEngine).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                InteropServiceAttribute attribute = method.GetCustomAttribute<InteropServiceAttribute>();
                if (attribute is null) continue;
                InteropDescriptor descriptor = new InteropDescriptor(attribute, method);
                services.Add(descriptor.Hash, descriptor);
            }
            foreach (NativeContract contract in NativeContract.Contracts)
            {
                InteropDescriptor descriptor = new InteropDescriptor(contract);
                services.Add(descriptor.Hash, descriptor);
            }
        }

        public ApplicationEngine(TriggerType trigger, IVerifiable container, StoreView snapshot, long gas, bool testMode = false)
        {
            this.gas_amount = GasFree + gas;
            this.testMode = testMode;
            this.Trigger = trigger;
            this.ScriptContainer = container;
            this.Snapshot = snapshot;
        }

        internal bool AddGas(long gas)
        {
            GasConsumed = checked(GasConsumed + gas);
            return testMode || GasConsumed <= gas_amount;
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

        public override void Dispose()
        {
            foreach (IDisposable disposable in disposables)
                disposable.Dispose();
            disposables.Clear();
            base.Dispose();
        }

        protected override void LoadContext(ExecutionContext context)
        {
            // Set default execution context state

            context.GetState<ExecutionContextState>().ScriptHash ??= ((byte[])context.Script).ToScriptHash();

            base.LoadContext(context);
        }

        public ExecutionContext LoadScript(Script script, CallFlags callFlags, int rvcount = -1)
        {
            ExecutionContext context = LoadScript(script, rvcount);
            context.GetState<ExecutionContextState>().CallFlags = callFlags;
            return context;
        }

        protected override bool OnSysCall(uint method)
        {
            if (!services.TryGetValue(method, out var descriptor))
                return false;
            if (!AddGas(descriptor.Price))
                return false;
            if (!descriptor.AllowedTriggers.HasFlag(Trigger))
                return false;
            if (!CurrentContext.GetState<ExecutionContextState>().CallFlags.HasFlag(descriptor.RequiredCallFlags))
                return false;
            return descriptor.Handler(this);
        }

        protected override bool PreExecuteInstruction()
        {
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            return AddGas(OpCodePrices[CurrentContext.CurrentInstruction.OpCode]);
        }

        public static ApplicationEngine Run(byte[] script, StoreView snapshot,
            IVerifiable container = null, Block persistingBlock = null, int offset = 0, bool testMode = false, long extraGAS = default)
        {
            snapshot.PersistingBlock = persistingBlock ?? snapshot.PersistingBlock ?? CreateDummyBlock(snapshot);
            ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, container, snapshot, extraGAS, testMode);
            engine.LoadScript(script).InstructionPointer = offset;
            engine.Execute();
            return engine;
        }

        public static ApplicationEngine Run(byte[] script, IVerifiable container = null, Block persistingBlock = null, int offset = 0, bool testMode = false, long extraGAS = default)
        {
            using (SnapshotView snapshot = Blockchain.Singleton.GetSnapshot())
            {
                return Run(script, snapshot, container, persistingBlock, offset, testMode, extraGAS);
            }
        }

        internal void SendNotification(UInt160 script_hash, StackItem state)
        {
            NotifyEventArgs notification = new NotifyEventArgs(ScriptContainer, script_hash, state);
            Notify?.Invoke(this, notification);
            notifications.Add(notification);
        }

        public bool TryPop(out string s)
        {
            if (TryPop(out ReadOnlySpan<byte> b))
            {
                s = Encoding.UTF8.GetString(b);
                return true;
            }
            else
            {
                s = default;
                return false;
            }
        }
    }
}
