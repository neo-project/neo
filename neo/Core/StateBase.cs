using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using System;
using System.IO;

namespace Neo.Core
{
    public abstract class StateBase : IInteropInterface, ISerializable
    {
        public const byte StateVersion = 0;

        public virtual int Size => sizeof(byte);

        public virtual void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != StateVersion) throw new FormatException();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(StateVersion);
        }

        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["version"] = StateVersion;
            return json;
        }
    }
}
