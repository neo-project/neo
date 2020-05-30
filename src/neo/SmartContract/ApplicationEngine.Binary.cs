using Neo.VM.Types;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Binary_Serialize = Register("System.Binary.Serialize", nameof(BinarySerialize), 0_00100000, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor System_Binary_Deserialize = Register("System.Binary.Deserialize", nameof(BinaryDeserialize), 0_00500000, TriggerType.All, CallFlags.None);

        internal byte[] BinarySerialize(StackItem item)
        {
            return BinarySerializer.Serialize(item, MaxItemSize);
        }

        internal StackItem BinaryDeserialize(byte[] data)
        {
            return BinarySerializer.Deserialize(data, MaxStackSize, MaxItemSize, ReferenceCounter);
        }
    }
}
