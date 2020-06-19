using Neo.VM.Types;

namespace Neo.SmartContract.Callbacks
{
    internal abstract class CallbackBase
    {
        public abstract int ParametersCount { get; }

        public abstract void LoadContext(ApplicationEngine engine, Array args);
    }
}
