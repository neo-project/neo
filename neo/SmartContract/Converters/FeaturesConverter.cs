using Neo.Ledger;
using Newtonsoft.Json;
using System;

namespace Neo.SmartContract.Converters
{
    public class FeaturesConverter : JsonConverter
    {
        class jsonFeature
        {
            public bool storage { get; set; }
            public bool payable { get; set; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ContractPropertyState);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var parsed = serializer.Deserialize<jsonFeature>(reader);
            var state = ContractPropertyState.NoProperty;

            if (parsed.storage) state |= ContractPropertyState.HasStorage;
            if (parsed.payable) state |= ContractPropertyState.Payable;

            return state;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var state = (ContractPropertyState)value;

            writer.WriteStartObject();

            writer.WritePropertyName("storage");
            writer.WriteValue(state.HasFlag(ContractPropertyState.HasStorage));

            writer.WritePropertyName("payable");
            writer.WriteValue(state.HasFlag(ContractPropertyState.Payable));

            writer.WriteEndObject();
        }
    }
}