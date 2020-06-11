namespace Neo.SmartContract.Callbacks
{
    internal class SyscallCallback : CallbackBase
    {
        public uint Method { get; }
        public override int ParametersCount => ApplicationEngine.Services[Method].Parameters.Length;

        public SyscallCallback(uint method) : base()
        {
            this.Method = method;
        }
    }
}
