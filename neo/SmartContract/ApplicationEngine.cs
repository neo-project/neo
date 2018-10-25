using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.SmartContract
{
    public class ApplicationEngine : ExecutionEngine
    {
        #region Limits
        /// <summary>
        /// Max value for SHL and SHR
        /// </summary>
        public const int Max_SHL_SHR = ushort.MaxValue;
        /// <summary>
        /// Min value for SHL and SHR
        /// </summary>
        public const int Min_SHL_SHR = -Max_SHL_SHR;
        /// <summary>
        /// Set the max size allowed size for BigInteger
        /// </summary>
        public const int MaxSizeForBigInteger = 32;
        /// <summary>
        /// Set the max Stack Size
        /// </summary>
        public const uint MaxStackSize = 2 * 1024;
        /// <summary>
        /// Set Max Item Size
        /// </summary>
        public const uint MaxItemSize = 1024 * 1024;
        /// <summary>
        /// Set Max Invocation Stack Size
        /// </summary>
        public const uint MaxInvocationStackSize = 1024;
        /// <summary>
        /// Set Max Array Size
        /// </summary>
        public const uint MaxArraySize = 1024;
        #endregion

        private const long ratio = 100000;
        private const long gas_free = 10 * 100000000;
        private readonly long gas_amount;
        private long gas_consumed = 0;
        private readonly bool testMode;
        private readonly Snapshot snapshot;

        private int stackitem_count = 0;
        private bool is_stackitem_count_strict = true;

        public Fixed8 GasConsumed => new Fixed8(gas_consumed);
        public new NeoService Service => (NeoService)base.Service;

        public ApplicationEngine(TriggerType trigger, IScriptContainer container, Snapshot snapshot, Fixed8 gas, bool testMode = false)
            : base(container, Cryptography.Crypto.Default, snapshot, new NeoService(trigger, snapshot))
        {
            this.gas_amount = gas_free + gas.GetData();
            this.testMode = testMode;
            this.snapshot = snapshot;
        }

        private bool CheckArraySize(OpCode nextInstruction)
        {
            int size;
            switch (nextInstruction)
            {
                case OpCode.PACK:
                case OpCode.NEWARRAY:
                case OpCode.NEWSTRUCT:
                    {
                        if (CurrentContext.EvaluationStack.Count == 0) return false;
                        size = (int)CurrentContext.EvaluationStack.Peek().GetBigInteger();
                    }
                    break;
                case OpCode.SETITEM:
                    {
                        if (CurrentContext.EvaluationStack.Count < 3) return false;
                        if (!(CurrentContext.EvaluationStack.Peek(2) is Map map)) return true;
                        StackItem key = CurrentContext.EvaluationStack.Peek(1);
                        if (key is ICollection) return false;
                        if (map.ContainsKey(key)) return true;
                        size = map.Count + 1;
                    }
                    break;
                case OpCode.APPEND:
                    {
                        if (CurrentContext.EvaluationStack.Count < 2) return false;
                        if (!(CurrentContext.EvaluationStack.Peek(1) is Array array)) return false;
                        size = array.Count + 1;
                    }
                    break;
                default:
                    return true;
            }
            return size <= MaxArraySize;
        }

        private bool CheckInvocationStack(OpCode nextInstruction)
        {
            switch (nextInstruction)
            {
                case OpCode.CALL:
                case OpCode.APPCALL:
                case OpCode.CALL_I:
                case OpCode.CALL_E:
                case OpCode.CALL_ED:
                    if (InvocationStack.Count >= MaxInvocationStackSize) return false;
                    return true;
                default:
                    return true;
            }
        }

        private bool CheckItemSize(OpCode nextInstruction)
        {
            switch (nextInstruction)
            {
                case OpCode.PUSHDATA4:
                    {
                        if (CurrentContext.InstructionPointer + 4 >= CurrentContext.Script.Length)
                            return false;
                        uint length = CurrentContext.Script.ToUInt32(CurrentContext.InstructionPointer + 1);
                        if (length > MaxItemSize) return false;
                        return true;
                    }
                case OpCode.CAT:
                    {
                        if (CurrentContext.EvaluationStack.Count < 2) return false;
                        int length = CurrentContext.EvaluationStack.Peek(0).GetByteArray().Length + CurrentContext.EvaluationStack.Peek(1).GetByteArray().Length;
                        if (length > MaxItemSize) return false;
                        return true;
                    }
                default:
                    return true;
            }
        }

        /// <summary>
        /// Check if the BigInteger is allowed for numeric operations
        /// </summary>
        /// <param name="value">Value</param>
        /// <returns>Return True if are allowed, otherwise False</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckBigInteger(BigInteger value)
        {
            return value.ToByteArray().Length <= MaxSizeForBigInteger;
        }

        /// <summary>
        /// Check if the BigInteger is allowed for numeric operations
        /// </summary> 
        private bool CheckBigIntegers(OpCode nextInstruction)
        {
            switch (nextInstruction)
            {
                case OpCode.SHL:
                    {
                        BigInteger ishift = CurrentContext.EvaluationStack.Peek(0).GetBigInteger();

                        if ((ishift > Max_SHL_SHR || ishift < Min_SHL_SHR))
                            return false;

                        BigInteger x = CurrentContext.EvaluationStack.Peek(1).GetBigInteger();

                        if (!CheckBigInteger(x << (int)ishift))
                            return false;

                        break;
                    }
                case OpCode.SHR:
                    {
                        BigInteger ishift = CurrentContext.EvaluationStack.Peek(0).GetBigInteger();

                        if ((ishift > Max_SHL_SHR || ishift < Min_SHL_SHR))
                            return false;

                        BigInteger x = CurrentContext.EvaluationStack.Peek(1).GetBigInteger();

                        if (!CheckBigInteger(x >> (int)ishift))
                            return false;

                        break;
                    }
                case OpCode.INC:
                    {
                        BigInteger x = CurrentContext.EvaluationStack.Peek().GetBigInteger();

                        if (!CheckBigInteger(x) || !CheckBigInteger(x + 1))
                            return false;

                        break;
                    }
                case OpCode.DEC:
                    {
                        BigInteger x = CurrentContext.EvaluationStack.Peek().GetBigInteger();

                        if (!CheckBigInteger(x) || (x.Sign <= 0 && !CheckBigInteger(x - 1)))
                            return false;

                        break;
                    }
                case OpCode.ADD:
                    {
                        BigInteger x2 = CurrentContext.EvaluationStack.Peek().GetBigInteger();
                        BigInteger x1 = CurrentContext.EvaluationStack.Peek(1).GetBigInteger();

                        if (!CheckBigInteger(x2) || !CheckBigInteger(x1) || !CheckBigInteger(x1 + x2))
                            return false;

                        break;
                    }
                case OpCode.SUB:
                    {
                        BigInteger x2 = CurrentContext.EvaluationStack.Peek().GetBigInteger();
                        BigInteger x1 = CurrentContext.EvaluationStack.Peek(1).GetBigInteger();

                        if (!CheckBigInteger(x2) || !CheckBigInteger(x1) || !CheckBigInteger(x1 - x2))
                            return false;

                        break;
                    }
                case OpCode.MUL:
                    {
                        BigInteger x2 = CurrentContext.EvaluationStack.Peek().GetBigInteger();
                        BigInteger x1 = CurrentContext.EvaluationStack.Peek(1).GetBigInteger();

                        int lx1 = x1.ToByteArray().Length;

                        if (lx1 > MaxSizeForBigInteger)
                            return false;

                        int lx2 = x2.ToByteArray().Length;

                        if ((lx1 + lx2) > MaxSizeForBigInteger)
                            return false;

                        break;
                    }
                case OpCode.DIV:
                    {
                        BigInteger x2 = CurrentContext.EvaluationStack.Peek().GetBigInteger();
                        BigInteger x1 = CurrentContext.EvaluationStack.Peek(1).GetBigInteger();

                        if (!CheckBigInteger(x2) || !CheckBigInteger(x1))
                            return false;

                        break;
                    }
                case OpCode.MOD:
                    {
                        BigInteger x2 = CurrentContext.EvaluationStack.Peek().GetBigInteger();
                        BigInteger x1 = CurrentContext.EvaluationStack.Peek(1).GetBigInteger();

                        if (!CheckBigInteger(x2) || !CheckBigInteger(x1))
                            return false;

                        break;
                    }
            }

            return true;
        }

        private bool CheckStackSize(OpCode nextInstruction)
        {
            if (nextInstruction <= OpCode.PUSH16)
                stackitem_count += 1;
            else
                switch (nextInstruction)
                {
                    case OpCode.JMPIF:
                    case OpCode.JMPIFNOT:
                    case OpCode.DROP:
                    case OpCode.NIP:
                    case OpCode.EQUAL:
                    case OpCode.BOOLAND:
                    case OpCode.BOOLOR:
                    case OpCode.CHECKMULTISIG:
                    case OpCode.REVERSE:
                    case OpCode.HASKEY:
                    case OpCode.THROWIFNOT:
                        stackitem_count -= 1;
                        is_stackitem_count_strict = false;
                        break;
                    case OpCode.XSWAP:
                    case OpCode.ROLL:
                    case OpCode.CAT:
                    case OpCode.LEFT:
                    case OpCode.RIGHT:
                    case OpCode.AND:
                    case OpCode.OR:
                    case OpCode.XOR:
                    case OpCode.ADD:
                    case OpCode.SUB:
                    case OpCode.MUL:
                    case OpCode.DIV:
                    case OpCode.MOD:
                    case OpCode.SHL:
                    case OpCode.SHR:
                    case OpCode.NUMEQUAL:
                    case OpCode.NUMNOTEQUAL:
                    case OpCode.LT:
                    case OpCode.GT:
                    case OpCode.LTE:
                    case OpCode.GTE:
                    case OpCode.MIN:
                    case OpCode.MAX:
                    case OpCode.CHECKSIG:
                    case OpCode.CALL_ED:
                    case OpCode.CALL_EDT:
                        stackitem_count -= 1;
                        break;
                    case OpCode.APPCALL:
                    case OpCode.TAILCALL:
                    case OpCode.NOT:
                    case OpCode.ARRAYSIZE:
                        is_stackitem_count_strict = false;
                        break;
                    case OpCode.SYSCALL:
                    case OpCode.PICKITEM:
                    case OpCode.SETITEM:
                    case OpCode.APPEND:
                    case OpCode.VALUES:
                        stackitem_count = int.MaxValue;
                        is_stackitem_count_strict = false;
                        break;
                    case OpCode.DUPFROMALTSTACK:
                    case OpCode.DEPTH:
                    case OpCode.DUP:
                    case OpCode.OVER:
                    case OpCode.TUCK:
                    case OpCode.NEWMAP:
                        stackitem_count += 1;
                        break;
                    case OpCode.XDROP:
                    case OpCode.REMOVE:
                        stackitem_count -= 2;
                        is_stackitem_count_strict = false;
                        break;
                    case OpCode.SUBSTR:
                    case OpCode.WITHIN:
                    case OpCode.VERIFY:
                        stackitem_count -= 2;
                        break;
                    case OpCode.UNPACK:
                        stackitem_count += (int)CurrentContext.EvaluationStack.Peek().GetBigInteger();
                        is_stackitem_count_strict = false;
                        break;
                    case OpCode.NEWARRAY:
                    case OpCode.NEWSTRUCT:
                        stackitem_count += ((Array)CurrentContext.EvaluationStack.Peek()).Count;
                        break;
                    case OpCode.KEYS:
                        stackitem_count += ((Array)CurrentContext.EvaluationStack.Peek()).Count;
                        is_stackitem_count_strict = false;
                        break;
                }
            if (stackitem_count <= MaxStackSize) return true;
            if (is_stackitem_count_strict) return false;
            stackitem_count = GetItemCount(InvocationStack.SelectMany(p => p.EvaluationStack.Concat(p.AltStack)));
            if (stackitem_count > MaxStackSize) return false;
            is_stackitem_count_strict = true;
            return true;
        }

        private bool CheckDynamicInvoke(OpCode nextInstruction)
        {
            switch (nextInstruction)
            {
                case OpCode.APPCALL:
                case OpCode.TAILCALL:
                    for (int i = CurrentContext.InstructionPointer + 1; i < CurrentContext.InstructionPointer + 21; i++)
                    {
                        if (CurrentContext.Script[i] != 0) return true;
                    }
                    // if we get this far it is a dynamic call
                    // now look at the current executing script
                    // to determine if it can do dynamic calls
                    return snapshot.Contracts[new UInt160(CurrentContext.ScriptHash)].HasDynamicInvoke;
                case OpCode.CALL_ED:
                case OpCode.CALL_EDT:
                    return snapshot.Contracts[new UInt160(CurrentContext.ScriptHash)].HasDynamicInvoke;
                default:
                    return true;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Service.Dispose();
        }

        public new bool Execute()
        {
            try
            {
                while (true)
                {
                    OpCode nextOpcode = CurrentContext.InstructionPointer >= CurrentContext.Script.Length ? OpCode.RET : CurrentContext.NextInstruction;
                    if (!PreStepInto(nextOpcode))
                    {
                        State |= VMState.FAULT;
                        return false;
                    }
                    StepInto();
                    if (State.HasFlag(VMState.HALT) || State.HasFlag(VMState.FAULT))
                        break;
                    if (!PostStepInto(nextOpcode))
                    {
                        State |= VMState.FAULT;
                        return false;
                    }
                }
            }
            catch
            {
                State |= VMState.FAULT;
                return false;
            }
            return !State.HasFlag(VMState.FAULT);
        }

        private static int GetItemCount(IEnumerable<StackItem> items)
        {
            Queue<StackItem> queue = new Queue<StackItem>(items);
            List<StackItem> counted = new List<StackItem>();
            int count = 0;
            while (queue.Count > 0)
            {
                StackItem item = queue.Dequeue();
                count++;
                switch (item)
                {
                    case Array array:
                        if (counted.Any(p => ReferenceEquals(p, array)))
                            continue;
                        counted.Add(array);
                        foreach (StackItem subitem in array)
                            queue.Enqueue(subitem);
                        break;
                    case Map map:
                        if (counted.Any(p => ReferenceEquals(p, map)))
                            continue;
                        counted.Add(map);
                        foreach (StackItem subitem in map.Values)
                            queue.Enqueue(subitem);
                        break;
                }
            }
            return count;
        }

        protected virtual long GetPrice(OpCode nextInstruction)
        {
            if (nextInstruction <= OpCode.PUSH16) return 0;
            switch (nextInstruction)
            {
                case OpCode.NOP:
                    return 0;
                case OpCode.APPCALL:
                case OpCode.TAILCALL:
                    return 10;
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
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length - 3)
                return 1;
            byte length = CurrentContext.Script[CurrentContext.InstructionPointer + 1];
            if (CurrentContext.InstructionPointer > CurrentContext.Script.Length - length - 2)
                return 1;
            string api_name = Encoding.ASCII.GetString(CurrentContext.Script, CurrentContext.InstructionPointer + 2, length);
            switch (api_name)
            {
                case "System.Runtime.CheckWitness":
                case "Neo.Runtime.CheckWitness":
                case "AntShares.Runtime.CheckWitness":
                    return 200;
                case "System.Blockchain.GetHeader":
                case "Neo.Blockchain.GetHeader":
                case "AntShares.Blockchain.GetHeader":
                    return 100;
                case "System.Blockchain.GetBlock":
                case "Neo.Blockchain.GetBlock":
                case "AntShares.Blockchain.GetBlock":
                    return 200;
                case "System.Blockchain.GetTransaction":
                case "Neo.Blockchain.GetTransaction":
                case "AntShares.Blockchain.GetTransaction":
                    return 100;
                case "System.Blockchain.GetTransactionHeight":
                case "Neo.Blockchain.GetTransactionHeight":
                    return 100;
                case "Neo.Blockchain.GetAccount":
                case "AntShares.Blockchain.GetAccount":
                    return 100;
                case "Neo.Blockchain.GetValidators":
                case "AntShares.Blockchain.GetValidators":
                    return 200;
                case "Neo.Blockchain.GetAsset":
                case "AntShares.Blockchain.GetAsset":
                    return 100;
                case "System.Blockchain.GetContract":
                case "Neo.Blockchain.GetContract":
                case "AntShares.Blockchain.GetContract":
                    return 100;
                case "Neo.Transaction.GetReferences":
                case "AntShares.Transaction.GetReferences":
                    return 200;
                case "Neo.Transaction.GetUnspentCoins":
                    return 200;
                case "Neo.Transaction.GetWitnesses":
                    return 200;
                case "Neo.Witness.GetInvocationScript":
                case "Neo.Witness.GetVerificationScript":
                    return 100;
                case "Neo.Account.IsStandard":
                    return 100;
                case "Neo.Asset.Create":
                case "AntShares.Asset.Create":
                    return 5000L * 100000000L / ratio;
                case "Neo.Asset.Renew":
                case "AntShares.Asset.Renew":
                    return (byte)CurrentContext.EvaluationStack.Peek(1).GetBigInteger() * 5000L * 100000000L / ratio;
                case "Neo.Contract.Create":
                case "Neo.Contract.Migrate":
                case "AntShares.Contract.Create":
                case "AntShares.Contract.Migrate":
                    long fee = 100L;

                    ContractPropertyState contract_properties = (ContractPropertyState)(byte)CurrentContext.EvaluationStack.Peek(3).GetBigInteger();

                    if (contract_properties.HasFlag(ContractPropertyState.HasStorage))
                    {
                        fee += 400L;
                    }
                    if (contract_properties.HasFlag(ContractPropertyState.HasDynamicInvoke))
                    {
                        fee += 500L;
                    }
                    return fee * 100000000L / ratio;
                case "System.Storage.Get":
                case "Neo.Storage.Get":
                case "AntShares.Storage.Get":
                    return 100;
                case "System.Storage.Put":
                case "System.Storage.PutEx":
                case "Neo.Storage.Put":
                case "AntShares.Storage.Put":
                    return ((CurrentContext.EvaluationStack.Peek(1).GetByteArray().Length + CurrentContext.EvaluationStack.Peek(2).GetByteArray().Length - 1) / 1024 + 1) * 1000;
                case "System.Storage.Delete":
                case "Neo.Storage.Delete":
                case "AntShares.Storage.Delete":
                    return 100;
                default:
                    return 1;
            }
        }

        private bool PostStepInto(OpCode nextOpcode)
        {
            if (!CheckStackSize(nextOpcode)) return false;
            return true;
        }

        private bool PreStepInto(OpCode nextOpcode)
        {
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            gas_consumed = checked(gas_consumed + GetPrice(nextOpcode) * ratio);
            if (!testMode && gas_consumed > gas_amount) return false;
            if (!CheckItemSize(nextOpcode)) return false;
            if (!CheckArraySize(nextOpcode)) return false;
            if (!CheckInvocationStack(nextOpcode)) return false;
            if (!CheckBigIntegers(nextOpcode)) return false;
            if (!CheckDynamicInvoke(nextOpcode)) return false;
            return true;
        }

        public static ApplicationEngine Run(byte[] script, Snapshot snapshot,
            IScriptContainer container = null, Block persistingBlock = null, bool testMode = false, Fixed8 extraGAS = default(Fixed8))
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
        
        public static ApplicationEngine Run(byte[] script, IScriptContainer container = null, Block persistingBlock = null, bool testMode = false, Fixed8 extraGAS = default(Fixed8))
        {
            using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
            {
                return Run(script, snapshot, container, persistingBlock, testMode, extraGAS);
            }
        }
    }
}
