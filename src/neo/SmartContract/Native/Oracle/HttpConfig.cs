using Neo.IO;
using System.IO;

namespace Neo.SmartContract.Native.Oracle
{
    public class HttpConfig : ISerializable
    {
        public const string Key = "HttpConfig";

        public static readonly string[] AllowedFormats = new string[]
        {
            "application/json"
        };

        #region Serializable properties

        public int TimeOut { get; set; } = 5000;

        #endregion

        public int Size => sizeof(int) + AllowedFormats.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            TimeOut = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TimeOut);
        }
    }
}
