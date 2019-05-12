using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    public class ApplicationEngine : ExecutionEngine
    {
        private const long ratio = 100000;
        private static readonly long gas_free = 10 * (long)NativeContract.GAS.Factor;
        private readonly long gas_amount;
        private readonly bool testMode;

        public long GasConsumed { get; private set; } = 0;
        public new NeoService Service => (NeoService)base.Service;

        public ApplicationEngine(TriggerType trigger, IScriptContainer container, Snapshot snapshot, long gas, bool testMode = false)
            : base(container, Cryptography.Crypto.Default, new NeoService(trigger, snapshot))
        {
            this.gas_amount = gas_free + gas;
            this.testMode = testMode;
        }

        public override void Dispose()
        {
            base.Dispose();
            Service.Dispose();
        }

        protected virtual long GetPrice()
        {
            Instruction instruction = CurrentContext.CurrentInstruction;
            if (instruction.OpCode <= OpCode.NOP) return 0;
            switch (instruction.OpCode)
            {
                case OpCode.SYSCALL:
                    return GetPriceForSysCall();
                case OpCode.SHA1:
                case OpCode.SHA256:
                    return 10;
                case OpCode.HASH160:
                case OpCode.HASH256:
                    return 20;
                case OpCode.CHECKSIG:
                case OpCode.VERIFY:
                    return 100;
                case OpCode.CHECKMULTISIG:
                    {
                        if (CurrentContext.EvaluationStack.Count == 0) return 1;

                        var item = CurrentContext.EvaluationStack.Peek();

                        int n;
                        if (item is Array array) n = array.Count;
                        else n = (int)item.GetBigInteger();

                        if (n < 1) return 1;
                        return 100 * n;
                    }
                default: return 1;
            }
        }

        protected virtual long GetPriceForSysCall()
        {
            Instruction instruction = CurrentContext.CurrentInstruction;
            uint api_hash = instruction.Operand.Length == 4
                ? instruction.TokenU32
                : instruction.TokenString.ToInteropMethodHash();
            long price = Service.GetPrice(api_hash);
            if (price > 0) return price;
            if (api_hash == "Neo.Contract.Create".ToInteropMethodHash() ||
                api_hash == "Neo.Contract.Migrate".ToInteropMethodHash())
            {
                long fee = 100L;

                ContractPropertyState contract_properties = (ContractPropertyState)(byte)CurrentContext.EvaluationStack.Peek(3).GetBigInteger();

                if (contract_properties.HasFlag(ContractPropertyState.HasStorage))
                {
                    fee += 400L;
                }
                return fee * (long)NativeContract.GAS.Factor / ratio;
            }
            if (api_hash == "System.Storage.Put".ToInteropMethodHash() ||
                api_hash == "System.Storage.PutEx".ToInteropMethodHash())
                return ((CurrentContext.EvaluationStack.Peek(1).GetByteArray().Length + CurrentContext.EvaluationStack.Peek(2).GetByteArray().Length - 1) / 1024 + 1) * 1000;
            return 1;
        }

        protected override bool PreExecuteInstruction()
        {
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            GasConsumed = checked(GasConsumed + GetPrice() * ratio);
            if (!testMode && GasConsumed > gas_amount) return false;
            return true;
        }

        public static ApplicationEngine Run(byte[] script, Snapshot snapshot,
            IScriptContainer container = null, Block persistingBlock = null, bool testMode = false, long extraGAS = default)
        {
            snapshot.PersistingBlock = persistingBlock ?? snapshot.PersistingBlock ?? new Block
            {
                Version = 0,
                PrevHash = snapshot.CurrentBlockHash,
                MerkleRoot = new UInt256(),
                Timestamp = snapshot.Blocks[snapshot.CurrentBlockHash].TrimmedBlock.Timestamp + Blockchain.SecondsPerBlock,
                Index = snapshot.Height + 1,
                ConsensusData = 0,
                NextConsensus = snapshot.Blocks[snapshot.CurrentBlockHash].TrimmedBlock.NextConsensus,
                Witness = new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                },
                Transactions = new Transaction[0]
            };
            ApplicationEngine engine = new ApplicationEngine(TriggerType.Application, container, snapshot, extraGAS, testMode);
            engine.LoadScript(script);
            engine.Execute();
            return engine;
        }

        public static ApplicationEngine Run(byte[] script, IScriptContainer container = null, Block persistingBlock = null, bool testMode = false, long extraGAS = default)
        {
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                return Run(script, snapshot, container, persistingBlock, testMode, extraGAS);
            }
        }
    }
}
