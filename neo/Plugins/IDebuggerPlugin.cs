using Neo.VM;

namespace Neo.Plugins
{
    public interface IDebuggerPlugin
    {
        bool DebuggerActive { get; }
        bool OnExecute(ExecutionEngine engine);
    }
}
