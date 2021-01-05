using Neo.IO;
using System;
using System.IO;

namespace Neo.SmartContract
{
    public class MethodToken : ISerializable
    {
        public UInt160 Hash;
        public string Method;
        public ushort ParametersCount;
        public ushort RVCount;
        public CallFlags CallFlags;

        public int Size =>
            UInt160.Length +        // Hash
            Method.GetVarSize() +   // Method
            sizeof(ushort) +        // ParametersCount
            sizeof(ushort) +        // RVCount
            sizeof(CallFlags);      // CallFlags

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Hash = reader.ReadSerializable<UInt160>();
            Method = reader.ReadVarString(32);
            ParametersCount = reader.ReadUInt16();
            RVCount = reader.ReadUInt16();
            CallFlags = (CallFlags)reader.ReadByte();
            if ((CallFlags & ~CallFlags.All) != 0)
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Hash);
            writer.WriteVarString(Method);
            writer.Write(ParametersCount);
            writer.Write(RVCount);
            writer.Write((byte)CallFlags);
        }
    }
}
