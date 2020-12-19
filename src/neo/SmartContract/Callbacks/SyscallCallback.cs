using System;

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

        public override void LoadContext(ApplicationEngine engine)
        {
        }
    }
}
