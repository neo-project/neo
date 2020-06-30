using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Oracle
{
    public class OracleRequest : IInteroperable
    {
        public string Url;
        public UInt256 Txid;
        public UInt160 CallbackContract;
        public string CallbackMethod;

        public void FromStackItem(StackItem stackItem)
        {
            Array array = (Array)stackItem;
            Url = array[0].GetString();
            Txid = new UInt256(array[1].GetSpan());
            CallbackContract = new UInt160(array[2].GetSpan());
            CallbackMethod = array[3].GetString();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter)
            {
                Url,
                Txid.ToArray(),
                CallbackContract.ToArray(),
                CallbackMethod
            };
        }
    }
}
