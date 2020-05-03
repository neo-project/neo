using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public abstract class TransactionAttribute : ISerializable
    {
        public abstract TransactionAttributeUsage Usage { get; }
        public abstract int Size { get; }
        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);

        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["usage"] = Usage
            };
        }
    }
}
