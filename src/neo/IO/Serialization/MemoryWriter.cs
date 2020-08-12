using System.IO;

namespace Neo.IO.Serialization
{
    public sealed class MemoryWriter : BinaryWriter
    {
        public MemoryWriter()
            : base(new MemoryStream(), Utility.StrictUTF8, false)
        {
        }

        public byte[] ToArray()
        {
            Flush();
            MemoryStream ms = (MemoryStream)BaseStream;
            return ms.ToArray();
        }
    }
}
