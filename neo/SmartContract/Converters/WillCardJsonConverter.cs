using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;

namespace Neo.SmartContract.Converters
{
    public class WillCardJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var array = serializer.Deserialize<T[]>(reader);
            return new WildCardContainer<T>(array);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var array = ((IEnumerable)value).Cast<T>().ToArray();
            serializer.Serialize(writer, array);
        }
    }
}