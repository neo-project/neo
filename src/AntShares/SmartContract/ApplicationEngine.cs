using AntShares.VM;
using System.Text;

namespace AntShares.SmartContract
{
    public class ApplicationEngine : ExecutionEngine
    {
        private const long ratio = 100000;
        private const long gas_free = 10 * 100000000;
        private long gas;

        public ApplicationEngine(IScriptContainer container, IScriptTable table, StateMachine state, Fixed8 gas)
            : base(container, Cryptography.Crypto.Default, table, state)
        {
            this.gas = gas_free + gas.GetData();
        }

        public new bool Execute()
        {
            while (!State.HasFlag(VMState.HALT) && !State.HasFlag(VMState.FAULT))
            {
                gas = checked(gas - GetPrice() * ratio);
                if (gas < 0) return false;
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
                case "AntShares.Blockchain.GetHeader": return 100;
                case "AntShares.Blockchain.GetBlock": return 200;
                case "AntShares.Blockchain.GetTransaction": return 100;
                case "AntShares.Blockchain.GetAccount": return 100;
                case "AntShares.Blockchain.GetAsset": return 100;
                case "AntShares.Transaction.GetReferences": return 200;
                case "AntShares.Account.SetVotes": return 1000;
                case "AntShares.Storage.Get": return 100;
                case "AntShares.Storage.Put": return 1000;
                case "AntShares.Storage.Delete": return 100;
                default: return 1;
            }
        }
    }
}
