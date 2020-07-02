using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Oracle
{
    public class OracleRequest : IInteroperable
    {
        public UInt256 Txid;
        public string Url;
        public string Filter;
        public UInt160 CallbackContract;
        public string CallbackMethod;
        public byte[] UserData;

        public void FromStackItem(StackItem stackItem)
        {
            Array array = (Array)stackItem;
            Txid = new UInt256(array[0].GetSpan());
            Url = array[1].GetString();
            Filter = array[2].GetString();
            CallbackContract = new UInt160(array[3].GetSpan());
            CallbackMethod = array[4].GetString();
            UserData = array[5].GetSpan().ToArray();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter)
            {
                Txid.ToArray(),
                Url,
                Filter ?? StackItem.Null,
                CallbackContract.ToArray(),
                CallbackMethod,
                UserData
            };
        }
    }
}
