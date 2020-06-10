namespace Neo.SmartContract
{
    internal class SyscallCallback : CallbackBase
    {
        public uint Method;

        public SyscallCallback(uint method) : base()
        {
            Method = method;
        }

        public override void Action(ApplicationEngine engine)
        {
            engine.RaiseOnSysCall(Method);
        }
    }
}
