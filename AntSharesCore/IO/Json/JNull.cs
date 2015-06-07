using System;

namespace AntShares.IO.Json
{
    internal class JNull : JObject
    {
        public static readonly JNull Value = new JNull();

        private JNull()
        {
        }

        public override bool AsBoolean()
        {
            return false;
        }

        public override string AsString()
        {
            return null;
        }

        public override bool CanConvertTo(Type type)
        {
            if (type == typeof(bool))
                return true;
            if (type == typeof(string))
                return true;
            return false;
        }

        public override string ToString()
        {
            return "null";
        }
    }
}
