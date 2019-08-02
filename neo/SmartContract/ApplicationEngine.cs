using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public partial class ApplicationEngine : ExecutionEngine
    {
        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        public const long GasFree = 0;
        private readonly long gas_amount;
        private readonly bool testMode;
        private readonly RandomAccessStack<UInt160> hashes = new RandomAccessStack<UInt160>();
        private readonly List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public TriggerType Trigger { get; }
        public IVerifiable ScriptContainer { get; }
        public Snapshot Snapshot { get; }
        public long GasConsumed { get; private set; } = 0;
        public UInt160 CurrentScriptHash => hashes.Count > 0 ? hashes.Peek() : null;
        public UInt160 CallingScriptHash => hashes.Count > 1 ? hashes.Peek(1) : null;
        public UInt160 EntryScriptHash => hashes.Count > 0 ? hashes.Peek(hashes.Count - 1) : null;
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;
        internal Dictionary<UInt160, int> InvocationCounter { get; } = new Dictionary<UInt160, int>();

        public ApplicationEngine(TriggerType trigger, IVerifiable container, Snapshot snapshot, long gas, bool testMode = false)
        {
            this.gas_amount = GasFree + gas;
            this.testMode = testMode;
            this.Trigger = trigger;
            this.ScriptContainer = container;
            this.Snapshot = snapshot;
            ContextLoaded += ApplicationEngine_ContextLoaded;
            ContextUnloaded += ApplicationEngine_ContextUnloaded;
        }

        internal T AddDisposable<T>(T disposable) where T : IDisposable
        {
            disposables.Add(disposable);
            return disposable;
        }

        private bool AddGas(long gas)
        {
            GasConsumed = checked(GasConsumed + gas);
            return testMode || GasConsumed <= gas_amount;
        }

        private void ApplicationEngine_ContextLoaded(object sender, ExecutionContext e)
        {
            hashes.Push(((byte[])e.Script).ToScriptHash());
        }

        private void ApplicationEngine_ContextUnloaded(object sender, ExecutionContext e)
        {
            hashes.Pop();
        }

        public override void Dispose()
        {
            foreach (IDisposable disposable in disposables)
                disposable.Dispose();
            disposables.Clear();
            base.Dispose();
        }

        protected override bool OnSysCall(uint method)
        {
            if (!AddGas(InteropService.GetPrice(method, CurrentContext.EvaluationStack)))
                return false;
            return InteropService.Invoke(this, method);
        }

        protected override bool PreExecuteInstruction()
        {
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            return AddGas(OpCodePrices[CurrentContext.CurrentInstruction.OpCode]);
        }

        private static Block CreateDummyBlock(Snapshot snapshot)
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
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                },
                ConsensusData = new ConsensusData(),
                Transactions = new Transaction[0]
            };
        }

        public static ApplicationEngine Run(byte[] script, Snapshot snapshot,
            IVerifiable container = null, Block persistingBlock = null, bool testMode = false, long extraGAS = default)
        {
            snapshot.PersistingBlock = persistingBlock ?? snapshot.PersistingBlock ?? CreateDummyBlock(snapshot);
            ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, container, snapshot, extraGAS, testMode);
            engine.LoadScript(script);
            engine.Execute();
            return engine;
        }

        public static ApplicationEngine Run(byte[] script, IVerifiable container = null, Block persistingBlock = null, bool testMode = false, long extraGAS = default)
        {
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                return Run(script, snapshot, container, persistingBlock, testMode, extraGAS);
            }
        }

        internal void SendLog(UInt160 script_hash, string message)
        {
            LogEventArgs log = new LogEventArgs(ScriptContainer, script_hash, message);
            Log?.Invoke(this, log);
        }

        internal void SendNotification(UInt160 script_hash, StackItem state)
        {
            NotifyEventArgs notification = new NotifyEventArgs(ScriptContainer, script_hash, state);
            Notify?.Invoke(this, notification);
            notifications.Add(notification);
        }
    }
}
