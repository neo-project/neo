using Neo.Ledger;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.NativeContract
{
    class NativeContract_Nep5Neo : INativeContract
    {
        public string Name => "Nep5Neo";

        public long Price => 1024;

        public ContractParameterType[] Parameter_list => new ContractParameterType[] {
            ContractParameterType.String,
            ContractParameterType.Array
        };

        public ContractParameterType Return_type => ContractParameterType.ByteArray;

        public ContractPropertyState Contract_properties => ContractPropertyState.HasStorage | ContractPropertyState.Payable;

        public string Version => "";

        public string Author => "";

        public string Email => "";

        public string Description => "";


        public bool Contract_Main(ExecutionEngine engine)
        {
            //throw new NotImplementedException();
            var trigger = (engine.Service as StandardService).Trigger;
            if (trigger == TriggerType.Verification)
            {
                StackItem returnvalue = false;
                engine.CurrentContext.EvaluationStack.Push(returnvalue);
                return true;
            }
            else if (trigger == TriggerType.Application)
            {
                var name = engine.CurrentContext.EvaluationStack.Pop().GetString();
                var _params = engine.CurrentContext.EvaluationStack.Pop() as Neo.VM.Types.Array;
                if (name == "name")
                {
                    return _Name(engine);
                }
                if (name == "decimal")
                {
                    return _Decimal(engine);
                }
                if (name == "balanceof")
                {
                    var from = _params[0].GetByteArray();
                    return _BalanceOf(engine, from);
                }
                if (name == "transfer")
                {
                    var from = _params[0].GetByteArray();
                    var to = _params[1].GetByteArray();
                    var value = _params[2].GetBigInteger();
                    return _Transfer(engine, from, to, value);
                }
            }
            ///其他的nep5接口，待實現
            return false;
        }
        bool _Name(ExecutionEngine engine)
        {
            StackItem returnvalue = System.Text.Encoding.UTF8.GetBytes(this.Name);
            engine.CurrentContext.EvaluationStack.Push(returnvalue);
            return true;
        }
        bool _Decimal(ExecutionEngine engine)
        {
            StackItem returnvalue = 8;
            engine.CurrentContext.EvaluationStack.Push(returnvalue);
            return true;
        }
        bool _BalanceOf(ExecutionEngine engine, byte[] from)
        {
            var blance = NativeContractTool.Storage_Get_Current(engine, from);
            StackItem returnvalue = blance.Value;
            engine.CurrentContext.EvaluationStack.Push(returnvalue);
            return true;
        }
        bool _Transfer(ExecutionEngine engine, byte[] from, byte[] to, System.Numerics.BigInteger value)
        {

            //直接操作存儲區
            var fromvalue = new System.Numerics.BigInteger(NativeContractTool.Storage_Get_Current(engine, from).Value);
            if (fromvalue < value)
                return false;
            NativeContractTool.Storage_Put_Current(engine, from, (fromvalue - value).ToByteArray());

            var tovalue = new System.Numerics.BigInteger(NativeContractTool.Storage_Get_Current(engine, from).Value);
            NativeContractTool.Storage_Put_Current(engine, to, (tovalue + value).ToByteArray());

            StackItem returnvalue = true;
            engine.CurrentContext.EvaluationStack.Push(returnvalue);
            return true;

        }
    }
}
