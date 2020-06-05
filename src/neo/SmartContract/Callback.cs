using Neo.VM.Types;

namespace Neo.SmartContract
{
    internal abstract class Callback
    {
        public readonly StackItem[] Arguments;

        public Callback(params StackItem[] args)
        {
            Arguments = args;
        }

        public abstract void Action(ApplicationEngine engine);
    }
}
