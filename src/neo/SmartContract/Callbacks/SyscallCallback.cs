using Neo.VM.Types;

namespace Neo.SmartContract.Callbacks
{
    internal class SyscallCallback : CallbackBase
    {
        public InteropDescriptor Method { get; }
        public override int ParametersCount => Method.Parameters.Length;

        public SyscallCallback(uint method)
        {
            this.Method = ApplicationEngine.Services[method];
        }

        public override void LoadContext(ApplicationEngine engine, Array args)
        {
            for (int i = args.Count - 1; i >= 0; i--)
                engine.Push(args[i]);
        }
    }
}
