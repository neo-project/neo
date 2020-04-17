using Neo.IO;
using System.IO;

namespace Neo.Oracle
{
    public class OracleFilter : ISerializable
    {
        /// <summary>
        /// Contract Hash
        /// </summary>
        public UInt160 ContractHash;

        /// <summary>
        /// You need a specific method for your filters
        /// </summary>
        public string FilterMethod;

        /// <summary>
        /// Filter args
        /// </summary>
        public string FilterArgs;

        public int Size => UInt160.Length + FilterMethod.GetVarSize() + FilterArgs.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            ContractHash = reader.ReadSerializable<UInt160>();
            FilterMethod = reader.ReadVarString(ushort.MaxValue);
            FilterArgs = reader.ReadVarString(ushort.MaxValue);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ContractHash);
            writer.WriteVarString(FilterMethod);
            writer.WriteVarString(FilterArgs);
        }
    }
}
