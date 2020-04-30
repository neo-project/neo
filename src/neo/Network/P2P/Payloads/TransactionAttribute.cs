using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public abstract class TransactionAttribute : ISerializable
    {
        public abstract TransactionAttributeUsage Usage { get; }
        public abstract int Size { get; }
        public abstract void Serialize(BinaryWriter writer);
        public abstract void Deserialize(BinaryReader reader);
        protected abstract JObject ToJsonValue();

        public JObject ToJson()
        {
            var ret = new JObject();
            ret["type"] = Usage;
            ret["value"] = ToJsonValue();
            return ret;
        }

        public static TransactionAttribute FromJson(JObject json)
        {
            switch (Enum.Parse(typeof(TransactionAttributeUsage), json["type"].AsString(), true))
            {
                case TransactionAttributeUsage.Cosigners:
                    {
                        return CosignerAttribute.FromJsonValue(json["value"]);
                    }
                default: throw new FormatException();
            }
        }
    }
}
