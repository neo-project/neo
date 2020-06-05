using Neo.VM.Types;

namespace Neo.SmartContract
{
    internal class SyscallCallback : Callback
    {
        public uint Method;

        public SyscallCallback(uint method, params StackItem[] args) : base(args)
        {
            Method = method;
        }

        public override void Action(ApplicationEngine engine)
        {
            // Copy arguments

            foreach (var arg in Arguments)
            {
                engine.Push(arg);
            }

            // Execute syscall

            engine.OnSysCall(Method);
        }
    }
}
