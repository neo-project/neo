using Neo.VM;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public class VerificationControl : GasControl
    {
        private readonly static List<OpCode> _bannedOpcodes = new List<OpCode>(new OpCode[] { /*OpCode.NOP*/ });

        public int MaxStepInto { get; private set; }

        public VerificationControl(int maxSteps, long gas) : base(gas, false)
        {
            MaxStepInto = maxSteps;
        }

        public override bool OnPreExecute(OpCode opCode)
        {
            if (!base.OnPreExecute(opCode)) return false;

            MaxStepInto--;

            return MaxStepInto > 0 && !_bannedOpcodes.Contains(opCode);
        }
    }
}