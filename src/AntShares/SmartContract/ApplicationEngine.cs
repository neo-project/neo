using AntShares.VM;
using System;
using System.Text;

namespace AntShares.SmartContract
{
    public class ApplicationEngine : ExecutionEngine
    {
        private const long ratio = 100000;
        private const long gas_free = 10 * 100000000;
        private long gas;

        public ApplicationEngine(IScriptContainer container, IScriptTable table, InteropService service, Fixed8 gas)
            : base(container, Cryptography.Crypto.Default, table, service)
        {
            this.gas = gas_free + gas.GetData();
        }

        private bool CheckArraySize()
        {
            const uint MaxArraySize = 1024;
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            OpCode opcode = CurrentContext.NextInstruction;
            switch (opcode)
            {
                case OpCode.PACK:
                case OpCode.NEWARRAY:
                    {
                        int size = (int)EvaluationStack.Peek().GetBigInteger();
                        if (size > MaxArraySize) return false;
                        return true;
                    }
                default:
                    return true;
            }
        }

        private bool CheckInvocationStack()
        {
            const uint MaxStackSize = 1024;
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            OpCode opcode = CurrentContext.NextInstruction;
            switch (opcode)
            {
                case OpCode.CALL:
                case OpCode.APPCALL:
                    if (InvocationStack.Count >= MaxStackSize) return false;
                    return true;
                default:
                    return true;
            }
        }

        private bool CheckItemSize()
        {
            const uint MaxItemSize = 1024 * 1024;
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            OpCode opcode = CurrentContext.NextInstruction;
            switch (opcode)
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
                        if (EvaluationStack.Count < 2) return false;
                        int length;
                        try
                        {
                            length = EvaluationStack.Peek(0).GetByteArray().Length + EvaluationStack.Peek(1).GetByteArray().Length;
                        }
                        catch (NotSupportedException)
                        {
                            return false;
                        }
                        if (length > MaxItemSize) return false;
                        return true;
                    }
                default:
                    return true;
            }
        }

        private bool CheckStackSize()
        {
            const uint MaxStackSize = 2 * 1024;
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return true;
            int size = 0;
            OpCode opcode = CurrentContext.NextInstruction;
            if (opcode <= OpCode.PUSH16)
                size = 1;
            else
                switch (opcode)
                {
                    case OpCode.DEPTH:
                    case OpCode.DUP:
                    case OpCode.OVER:
                    case OpCode.TUCK:
                        size = 1;
                        break;
                    case OpCode.UNPACK:
                        StackItem item = EvaluationStack.Peek();
                        if (!item.IsArray) return false;
                        size = item.GetArray().Length;
                        break;
                }
            if (size == 0) return true;
            size += EvaluationStack.Count + AltStack.Count;
            if (size > MaxStackSize) return false;
            return true;
        }

        public new bool Execute()
        {
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT))
            {
                try
                {
                    gas = checked(gas - GetPrice() * ratio);
                }
                catch (OverflowException)
                {
                    return false;
                }
                if (gas < 0) return false;
                if (!CheckItemSize()) return false;
                if (!CheckStackSize()) return false;
                if (!CheckArraySize()) return false;
                if (!CheckInvocationStack()) return false;
                StepInto();
            }
            return !State.HasFlag(VMState.FAULT);
        }

        protected virtual long GetPrice()
        {
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length)
                return 0;
            OpCode opcode = CurrentContext.NextInstruction;
            if (opcode <= OpCode.PUSH16) return 0;
            switch (opcode)
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
                    return 100;
                case OpCode.CHECKMULTISIG:
                    {
                        if (EvaluationStack.Count == 0) return 1;
                        int n = (int)EvaluationStack.Peek().GetBigInteger();
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
            if (CurrentContext.InstructionPointer >= CurrentContext.Script.Length - length - 2)
                return 1;
            string api_name = Encoding.ASCII.GetString(CurrentContext.Script, CurrentContext.InstructionPointer + 2, length);
            switch (api_name)
            {
                case "AntShares.Runtime.CheckWitness": return 200;
                case "AntShares.Blockchain.GetHeader": return 100;
                case "AntShares.Blockchain.GetBlock": return 200;
                case "AntShares.Blockchain.GetTransaction": return 100;
                case "AntShares.Blockchain.GetAccount": return 100;
                case "AntShares.Blockchain.GetValidators": return 200;
                case "AntShares.Blockchain.GetAsset": return 100;
                case "AntShares.Blockchain.GetContract": return 100;
                case "AntShares.Transaction.GetReferences": return 200;
                case "AntShares.Account.SetVotes": return 1000;
                case "AntShares.Validator.Register": return 1000L * 100000000L / ratio;
                case "AntShares.Asset.Create": return 5000L * 100000000L / ratio;
                case "AntShares.Asset.Renew": return (byte)EvaluationStack.Peek(1).GetBigInteger() * 5000L * 100000000L / ratio;
                case "AntShares.Contract.Create":
                case "AntShares.Contract.Migrate": return 500L * 100000000L / ratio;
                case "AntShares.Storage.Get": return 100;
                case "AntShares.Storage.Put": return ((EvaluationStack.Peek(1).GetByteArray().Length + EvaluationStack.Peek(2).GetByteArray().Length - 1) / 1024 + 1) * 1000;
                case "AntShares.Storage.Delete": return 100;
                default: return 1;
            }
        }
    }
}
