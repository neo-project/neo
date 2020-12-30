using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.SmartContract
{
    public class MethodToken : ISerializable
    {
        public UInt160 Hash;
        public string Method;
        public ushort ParametersCount;
        public bool HasReturnValue;
        public CallFlags CallFlags;

        public int Size =>
            UInt160.Length +        // Hash
            Method.GetVarSize() +   // Method
            sizeof(ushort) +        // ParametersCount
            sizeof(bool) +          // HasReturnValue
            sizeof(CallFlags);      // CallFlags

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Hash = reader.ReadSerializable<UInt160>();
            Method = reader.ReadVarString(32);
            if (Method.StartsWith('_')) throw new FormatException();
            ParametersCount = reader.ReadUInt16();
            HasReturnValue = reader.ReadBoolean();
            CallFlags = (CallFlags)reader.ReadByte();
            if ((CallFlags & ~CallFlags.All) != 0) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Hash);
            writer.WriteVarString(Method);
            writer.Write(ParametersCount);
            writer.Write(HasReturnValue);
            writer.Write((byte)CallFlags);
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["hash"] = Hash.ToString(),
                ["method"] = Method,
                ["paramcount"] = ParametersCount,
                ["hasreturnvalue"] = HasReturnValue,
                ["callflags"] = CallFlags
            };
        }
    }
}
