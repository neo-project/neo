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
        protected abstract void ToJson(JObject json);

        public JObject ToJson()
        {
            var ret = new JObject();
            ret["usage"] = Usage.ToString();
            ToJson(ret);
            return ret;
        }
    }
}
