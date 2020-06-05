using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    internal class PointerCallback : Callback
    {
        public ExecutionContext Context;
        public Pointer Pointer;

        public PointerCallback(ExecutionContext context, Pointer pointer, params StackItem[] args) : base(args)
        {
            Context = context;
            Pointer = pointer;
        }

        public override void Action(ApplicationEngine engine)
        {
            // Clone context

            var newContext = Context.Clone(0);
            newContext.InstructionPointer = Pointer.Position;

            // Copy arguments

            foreach (var arg in Arguments)
            {
                newContext.EvaluationStack.Push(arg);
            }

            // Load context

            engine.LoadContext(newContext);
        }
    }
}
