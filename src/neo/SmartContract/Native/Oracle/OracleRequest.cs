using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using System.IO;

namespace Neo.SmartContract.Native.Tokens
{
    public class OracleRequest : IInteroperable
    {
        public UInt256 RequestTxHash;
        public string Url;
        public string FilterPath;
        public UInt160 CallbackContract;
        public string CallbackMethod;
        public uint ValidHeight;
        public long OracleFee;
        public RequestStatusType Status;

        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            RequestTxHash = @struct[0].GetSpan().AsSerializable<UInt256>();
            Url = ((Struct)stackItem)[1].GetString();
            FilterPath = @struct[2].GetString();
            CallbackContract = @struct[3].GetSpan().AsSerializable<UInt160>();
            CallbackMethod = @struct[4].GetString();
            ValidHeight = (uint)@struct[5].GetInteger();
            OracleFee = (long)@struct[6].GetInteger();
            Status = (RequestStatusType)@struct[7].GetSpan().ToArray()[0];
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter)
            {
                RequestTxHash.ToArray(),
                Url,
                FilterPath,
                CallbackContract.ToArray(),
                CallbackMethod,
                ValidHeight,
                OracleFee,
                new byte[]{ (byte)Status }
            };
        }
    }
}
