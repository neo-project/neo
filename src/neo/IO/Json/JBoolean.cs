using System.Text.Json;

namespace Neo.IO.Json
{
    public class JBoolean : JObject
    {
        public bool Value { get; private set; }

        public JBoolean(bool value = false)
        {
            this.Value = value;
        }

        public override bool AsBoolean()
        {
            return Value;
        }

        public override double AsNumber()
        {
            return Value ? 1 : 0;
        }

        public override string AsString()
        {
            return Value.ToString().ToLowerInvariant();
        }

        public override bool GetBoolean() => Value;

        public override string ToString()
        {
            return AsString();
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteBooleanValue(Value);
        }

        public override JObject Clone()
        {
            return this;
        }

        public static implicit operator JBoolean(bool value)
        {
            return new JBoolean(value);
        }
    }
}
