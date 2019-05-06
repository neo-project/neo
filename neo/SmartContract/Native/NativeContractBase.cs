using Neo.VM;

namespace Neo.SmartContract.Native
{
    public abstract class NativeContractBase
    {
        protected static byte[] CreateNativeScript(string serviceName)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(serviceName);
                return sb.ToArray();
            }
        }
    }
}
