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

        private readonly RandomAccessStack<UInt160> hashes = new RandomAccessStack<UInt160>();
        private readonly List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public TriggerType Trigger { get; }
        public IVerifiable ScriptContainer { get; }
        public IExecutionControl Control { get; }
        public Snapshot Snapshot { get; }
        public UInt160 CurrentScriptHash => hashes.Count > 0 ? hashes.Peek() : null;
        public UInt160 CallingScriptHash => hashes.Count > 1 ? hashes.Peek(1) : null;
        public UInt160 EntryScriptHash => hashes.Count > 0 ? hashes.Peek(hashes.Count - 1) : null;
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;

        public ApplicationEngine(TriggerType trigger, IVerifiable container, Snapshot snapshot, IExecutionControl control = null)
        {
            this.Control = control;
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
            if (Control != null && !Control.OnSysCall(method, this)) return false;

            return InteropService.Invoke(this, method);
        }

        protected override bool PreExecuteInstruction()
        {
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;

            return Control == null || Control.OnPreExecute(CurrentContext.CurrentInstruction.OpCode);
        }

        public static ApplicationEngine Run(byte[] script, Snapshot snapshot,
            IVerifiable container = null, Block persistingBlock = null, IExecutionControl control = null)
        {
            snapshot.PersistingBlock = persistingBlock ?? snapshot.PersistingBlock ?? new Block
            {
                Version = 0,
                PrevHash = snapshot.CurrentBlockHash,
                MerkleRoot = new UInt256(),
                Timestamp = snapshot.Blocks[snapshot.CurrentBlockHash].Timestamp + Blockchain.SecondsPerBlock,
                Index = snapshot.Height + 1,
                NextConsensus = snapshot.Blocks[snapshot.CurrentBlockHash].NextConsensus,
                Witness = new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                },
                ConsensusData = new ConsensusData(),
                Transactions = new Transaction[0]
            };
            ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, container, snapshot, control);
            engine.LoadScript(script);
            engine.Execute();
            return engine;
        }

        public static ApplicationEngine Run(byte[] script, IVerifiable container = null, Block persistingBlock = null, IExecutionControl control = null)
        {
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                return Run(script, snapshot, container, persistingBlock, control);
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
