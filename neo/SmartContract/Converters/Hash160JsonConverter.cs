using Newtonsoft.Json;
using System;

namespace Neo.SmartContract.Converters
{
    public class Hash160JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.ValueType != typeof(string)) throw new FormatException();

            return UInt160.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((UInt160)value).ToString());
        }
    }
}