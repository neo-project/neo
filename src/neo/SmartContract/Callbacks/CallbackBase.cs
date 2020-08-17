using Neo.VM.Types;

namespace Neo.SmartContract.Callbacks
{
    public abstract class CallbackBase
    {
        public abstract int ParametersCount { get; }

        public abstract void LoadContext(ApplicationEngine engine, Array args);
    }
}
