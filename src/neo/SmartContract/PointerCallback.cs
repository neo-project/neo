using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    internal class PointerCallback : CallbackBase
    {
        public readonly ExecutionContext Context;
        public readonly Pointer Pointer;
        public readonly int ParCount;

        public PointerCallback(ExecutionContext context, Pointer pointer, int parCount)
        {
            Context = context;
            Pointer = pointer;
            ParCount = parCount;
        }

        public override void Action(ApplicationEngine engine)
        {
            // Clone context and seek to pointer position

            var newContext = Context.Clone(0);
            newContext.InstructionPointer = Pointer.Position;

            // Copy arguments

            for (int x = 0; x < ParCount; x++)
            {
                newContext.EvaluationStack.Push(engine.Pop());
            }

            // Load context

            engine.RaiseLoadContext(newContext);
        }
    }
}
