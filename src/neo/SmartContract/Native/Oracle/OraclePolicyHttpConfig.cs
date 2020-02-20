using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.SmartContract.Native.Oracle
{
    public class OraclePolicyHttpConfig : ISerializable
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
    }
}
