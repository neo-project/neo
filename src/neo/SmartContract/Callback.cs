using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    internal class Callback
    {
        public readonly Pointer Pointer;
        public readonly ExecutionContext Context;
        public readonly int RVcount;
        public readonly StackItem[] Params;

        public Callback(ExecutionContext context, Pointer pointer, int parcount, int rvcount)
        {
            Context = context;
            Pointer = pointer;
            RVcount = rvcount;
            Params = new StackItem[parcount];

            for (int x = 0; x < parcount; x++)
            {
                Params[x] = context.EvaluationStack.Pop();
            }
        }
    }
}
