using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Callbacks
{
    public class SyscallCallback : CallbackBase
    {
        public InteropDescriptor Method { get; }
        public override int ParametersCount => Method.Parameters.Count;

        public SyscallCallback(uint method, bool check = true)
        {
            this.Method = ApplicationEngine.Services[method];
            if (check && !Method.AllowCallback)
                throw new InvalidOperationException("This SYSCALL is not allowed for creating callback.");
        }

        public override void LoadContext(ApplicationEngine engine, Array args)
        {
            for (int i = args.Count - 1; i >= 0; i--)
                engine.Push(args[i]);
        }
    }
}
