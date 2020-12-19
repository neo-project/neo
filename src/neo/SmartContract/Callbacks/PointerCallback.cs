using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Callbacks
{
    public class PointerCallback : CallbackBase
    {
        private readonly ExecutionContext context;
        private readonly int pointer;

        public override int ParametersCount { get; }

        public PointerCallback(ExecutionContext context, Pointer pointer, int parametersCount)
        {
            this.context = context;
            this.pointer = pointer.Position;
            this.ParametersCount = parametersCount;
        }

        public override void LoadContext(ApplicationEngine engine)
        {
            StackItem[] args = new StackItem[ParametersCount];
            for (int i = 0; i < args.Length; i++)
                args[i] = engine.Pop();
            engine.LoadClonedContext(context.Clone(pointer));
            for (int i = args.Length - 1; i >= 0; i--)
                engine.Push(args[i]);
        }
    }
}
