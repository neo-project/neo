using Neo.VM;

namespace Neo.SmartContract
{
    public partial class GasControl : IExecutionControl
    {
        public const long GasFree = 0;

        private readonly long gas_amount;
        private readonly bool testMode;

        public long GasConsumed { get; private set; } = 0;

        public GasControl(long gas = 0, bool testMode = false)
        {
            this.gas_amount = GasFree + gas;
            this.testMode = testMode;
        }

        private bool AddGas(long gas)
        {
            GasConsumed = checked(GasConsumed + gas);
            return testMode || GasConsumed <= gas_amount;
        }

        public virtual bool OnPreExecute(OpCode opCode) => AddGas(OpCodePrices[opCode]);

        public bool OnSysCall(uint method, ApplicationEngine engine)
        {
            return AddGas(InteropService.GetPrice(method, engine.CurrentContext.EvaluationStack));
        }
    }
}