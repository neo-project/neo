using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        [InteropService("System.Binary.Serialize", 0_00100000, TriggerType.All, CallFlags.None)]
        private bool Binary_Serialize()
        {
            if (!TryPop(out StackItem item)) return false;
            Push(BinarySerializer.Serialize(item, MaxItemSize));
            return true;
        }

        [InteropService("System.Binary.Deserialize", 0_00500000, TriggerType.All, CallFlags.None)]
        private bool Binary_Deserialize()
        {
            if (!TryPop(out ReadOnlySpan<byte> data)) return false;
            Push(BinarySerializer.Deserialize(data, MaxStackSize, MaxItemSize, ReferenceCounter));
            return true;
        }
    }
}
