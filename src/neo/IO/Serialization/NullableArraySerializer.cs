using System.IO;

namespace Neo.IO.Serialization
{
    public class NullableArraySerializer<T> : ArraySerializer<T> where T : Serializable
    {
        protected override T DeserializeElement(BinaryReader reader)
        {
            return reader.ReadBoolean() ? base.DeserializeElement(reader) : null;
        }

        protected override void SerializeElement(BinaryWriter writer, T value)
        {
            bool isNull = value is null;
            writer.Write(!isNull);
            if (!isNull) base.SerializeElement(writer, value);
        }
    }
}
