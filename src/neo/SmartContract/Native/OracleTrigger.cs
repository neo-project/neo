using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{

    /// <summary>
    /// Represents an Oracle request in smart contracts.
    /// </summary>
    public class OracleTrigger : IInteroperable
    {
        /// <summary>
        /// The original transaction that sent the related request.
        /// </summary>
        public UInt256 OriginalTxid;

        /// <summary>
        /// The maximum amount of GAS that can be used when executing response callback.
        /// </summary>
        public long GasForResponse;

        /// <summary>
        /// The trigger height of the request.
        /// </summary>
        public uint Height;

        /// <summary>
        /// The hash of the callback contract.
        /// </summary>
        public UInt160 CallbackContract;

        /// <summary>
        /// The name of the callback method.
        /// </summary>
        public string CallbackMethod;

        /// <summary>
        /// The user-defined object that will be passed to the callback.
        /// </summary>
        public byte[] UserData;

        public void FromStackItem(StackItem stackItem)
        {
            Array array = (Array)stackItem;
            OriginalTxid = new UInt256(array[0].GetSpan());
            GasForResponse = (long)array[1].GetInteger();
            Height = (uint)array[2].GetInteger();
            CallbackContract = new UInt160(array[3].GetSpan());
            CallbackMethod = array[4].GetString();
            UserData = array[5].GetSpan().ToArray();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter)
            {
                OriginalTxid.ToArray(),
                GasForResponse,
                Height,
                CallbackContract.ToArray(),
                CallbackMethod,
                UserData
            };
        }
    }
}
