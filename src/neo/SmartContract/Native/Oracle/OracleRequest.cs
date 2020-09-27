using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Oracle
{
    public class OracleRequest : IInteroperable
    {
        public UInt256 OriginalTxid;
        public long GasForResponse;
        public string Url;
        public string Filter;
        public UInt160 CallbackContract;
        public string CallbackMethod;
        public byte[] UserData;

        public void FromStackItem(StackItem stackItem)
        {
            Array array = (Array)stackItem;
            OriginalTxid = new UInt256(array[0].GetSpan());
            GasForResponse = (long)array[1].GetInteger();
            Url = array[2].GetString();
            Filter = array[3].GetString();
            CallbackContract = new UInt160(array[4].GetSpan());
            CallbackMethod = array[5].GetString();
            UserData = array[6].GetSpan().ToArray();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter)
            {
                OriginalTxid.ToArray(),
                GasForResponse,
                Url,
                Filter ?? StackItem.Null,
                CallbackContract.ToArray(),
                CallbackMethod,
                UserData
            };
        }
    }
}
