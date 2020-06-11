using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Callbacks
{
    internal class PointerCallback : CallbackBase
    {
        private readonly ExecutionContext context;
        private readonly Pointer pointer;

        public override int ParametersCount { get; }

        public PointerCallback(ExecutionContext context, Pointer pointer, int parametersCount)
        {
            this.context = context;
            this.pointer = pointer;
            this.ParametersCount = parametersCount;
        }

        public ExecutionContext GetContext()
        {
            ExecutionContext newContext = context.Clone(0);
            newContext.InstructionPointer = pointer.Position;
            return newContext;
        }
    }
}
