using Neo.VM;

namespace Neo.SmartContract
{
    public interface IExecutionControl
    {
        bool OnPreExecute(OpCode opCode);
        bool OnSysCall(uint method, ApplicationEngine engine);
    }
}