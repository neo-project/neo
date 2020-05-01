using Neo.IO;
using Neo.IO.Caching;
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
        protected abstract void Serialize(JObject json);
        protected abstract void Deserialize(JObject json);

        public JObject ToJson()
        {
            var ret = new JObject();
            ret["usage"] = Usage.ToString();
            Serialize(ret);
            return ret;
        }

        public static TransactionAttribute FromJson(JObject json)
        {
            var usage = (TransactionAttributeUsage)Enum.Parse(typeof(TransactionAttributeUsage), json["usage"].AsString(), true);
            var obj = (TransactionAttribute)ReflectionCache<TransactionAttributeUsage>.CreateInstance(usage);
            obj.Deserialize(json);
            return obj;
        }
    }
}
