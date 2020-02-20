using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.SmartContract.Native.Oracle
{
    public class OracleHttpConfig : ISerializable, IInteroperable
    {
        public int Timeout { get; set; }

        public int Size => sizeof(int);

        public void Deserialize(BinaryReader reader)
        {
            Timeout = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Timeout);
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new VM.Types.Array(referenceCounter, new StackItem[]{Timeout});
        }
    }
}
