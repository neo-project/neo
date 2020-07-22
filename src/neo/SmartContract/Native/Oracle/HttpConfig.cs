using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native.Oracle
{
    public class HttpConfig : IInteroperable
    {
        public const string Key = "HttpConfig";

        public static readonly string[] AllowedFormats = new string[]
        {
            "application/json"
        };

        #region Serializable properties

        public int TimeOut { get; set; } = 5000;

        #endregion

        public void FromStackItem(StackItem stackItem)
        {
            var array = (VM.Types.Array)stackItem;

            TimeOut = (int)array[0].GetInteger();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new VM.Types.Array(referenceCounter)
            {
                TimeOut
            };
        }
    }
}
