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

        public override void LoadContext(ApplicationEngine engine, Array args)
        {
            engine.LoadContext(context.Clone(pointer), true);
            for (int i = args.Count - 1; i >= 0; i--)
                engine.Push(args[i]);
        }
    }
}
