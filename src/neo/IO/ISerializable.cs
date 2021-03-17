using System.IO;

namespace Neo.IO
{
    /// <summary>
    /// Represents NEO objects that can be serialized.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// The size of the object in bytes after serialization.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Serializes the object using the specified <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        void Serialize(BinaryWriter writer);

        /// <summary>
        /// Deserializes the object using the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        void Deserialize(BinaryReader reader);
    }
}
